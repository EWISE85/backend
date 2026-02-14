using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;

namespace ElecWasteCollection.Application.Helpers
{

    public class RouteOptimizer
    {
        /// <summary>
        /// Giải bài toán VRP với mục tiêu:
        /// 1. BẮT BUỘC đi qua tất cả các điểm (Penalty cực lớn nếu bỏ).
        /// 2. TỐI ƯU QUÃNG ĐƯỜNG (Tiết kiệm nhiên liệu) là mục tiêu chính.
        /// 3. LINH HOẠT THỜI GIAN: Cố gắng đến đúng giờ, nhưng chấp nhận trễ nếu giúp đường đi ngắn hơn nhiều.
        /// </summary>
        public static List<int> SolveVRP(
            long[,] matrixDist, long[,] matrixTime,
            List<OptimizationNode> nodes,
            double capKg, double capM3,
            TimeOnly shiftStart, TimeOnly shiftEnd)
        {
            int count = matrixDist.GetLength(0);
            if (count == 0) return new List<int>();

            var allIndices = Enumerable.Range(0, nodes.Count).ToList();

            try
            {
                // 1. Khởi tạo không gian bài toán
                // Node 0 là Depot (Kho/Trạm), các node 1..n là điểm lấy hàng
                RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
                RoutingModel routing = new RoutingModel(manager);

                // Mục tiêu: Giảm thiểu tổng mét đường xe chạy -> Tiết kiệm xăng
                int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                {
                    var from = manager.IndexToNode(i);
                    var to = manager.IndexToNode(j);
                    return matrixDist[from, to];
                });
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);


                // --- CẤU HÌNH THỜI GIAN (TIME WINDOWS) ---
                int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                {
                    int from = manager.IndexToNode(i);
                    int to = manager.IndexToNode(j);
                    long travel = (long)Math.Ceiling(matrixTime[from, to] / 60.0);
                    long service = (from == 0) ? 0 : 15;
                    return travel + service;
                });

                // Horizon: Tổng thời gian ca làm + 8 tiếng tăng ca (Overtime)
                // Để đảm bảo dù kẹt xe hay quá tải thì xe vẫn chạy tiếp chứ không cắt ngang.
                long shiftDuration = (long)(shiftEnd - shiftStart).TotalMinutes;
                long horizon = shiftDuration + 480;

                routing.AddDimension(
                    timeCallbackIndex,
                    10000,   // Slack (thời gian chờ tối đa tại 1 điểm)
                    horizon, // Tổng thời gian tối đa của lộ trình
                    false,   // Start cumul to zero
                    "Time");

                var timeDim = routing.GetMutableDimension("Time");
                timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, 0);

                for (int i = 0; i < nodes.Count; i++)
                {
                    long index = manager.NodeToIndex(i + 1);
                    var node = nodes[i];

                    // Chuyển đổi giờ hẹn của khách sang phút tính từ lúc bắt đầu ca
                    long startMin = Math.Max(0, (long)(node.Start - shiftStart).TotalMinutes);
                    long endMin = Math.Min(horizon, (long)(node.End - shiftStart).TotalMinutes);

                    // Fix logic nếu dữ liệu lỗi
                    if (endMin <= startMin) { startMin = 0; endMin = horizon; }

                    // 1. Ràng buộc cứng: Không được đến SỚM hơn giờ mở cửa
                    timeDim.CumulVar(index).SetMin(startMin);

                    // 2. Ràng buộc mềm: NÊN đến trước giờ đóng cửa
                    timeDim.SetCumulVarSoftUpperBound(index, endMin, 2000);

                    // 3. Ràng buộc cứng nhất: KHÔNG ĐƯỢC BỎ ĐƠN
                    routing.AddDisjunction(new long[] { index }, 1_000_000_000);
                }

                // --- CẤU HÌNH TẢI TRỌNG (CAPACITY) ---
                int weightCallback = routing.RegisterUnaryTransitCallback((long i) =>
                {
                    int node = manager.IndexToNode(i);
                    return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
                });
                routing.AddDimension(weightCallback, 0, (long)(capKg * 100 * 1.5), true, "Weight");

                int volumeCallback = routing.RegisterUnaryTransitCallback((long i) =>
                {
                    int node = manager.IndexToNode(i);
                    return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
                });
                routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000 * 1.5), true, "Volume");

                // --- CHIẾN LƯỢC TÌM KIẾM (SEARCH STRATEGY) ---
                RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();

                // Chiến lược 1: Chọn cung đường rẻ nhất để khởi tạo
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

                // Chiến lược 2: GUIDED LOCAL SEARCH (Quan trọng để tối ưu xăng)
                // Nó giúp thoát khỏi các bẫy cục bộ để tìm đường ngắn hơn nữa.
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;

                // Thời gian suy nghĩ: 3 giây (đủ cho < 100 điểm)
                searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 3 };

                // --- GIẢI ---
                Assignment solution = routing.SolveWithParameters(searchParameters);

                if (solution != null)
                {
                    var optimizedIndices = new List<int>();
                    long index = routing.Start(0);

                    while (!routing.IsEnd(index))
                    {
                        int node = manager.IndexToNode(index);
                        if (node != 0) optimizedIndices.Add(node - 1); 
                        index = solution.Value(routing.NextVar(index));
                    }

                    // --- LƯỚI VÉT (FALLBACK) ---
                    // Kiểm tra xem có node nào bị rớt lại không (dù rất khó xảy ra với penalty 1 tỷ)
                    // Nếu có, cưỡng chế nối vào đuôi để đảm bảo đủ 100% đơn hàng.
                    var missingIndices = allIndices.Except(optimizedIndices).ToList();
                    if (missingIndices.Any())
                    {
                        optimizedIndices.AddRange(missingIndices);
                    }

                    return optimizedIndices;
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                Console.WriteLine($"[OR-TOOLS Error] {ex.Message}");
            }

            // Nếu mọi thứ thất bại, trả về danh sách gốc để app không bị crash
            return allIndices;
        }
    }

    public class OptimizationNode
    {
        public int OriginalIndex { get; set; }
        public double Weight { get; set; }
        public double Volume { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
