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
    }
}

//public class OptimizationNode
//{
//    public int OriginalIndex { get; set; }
//    public double Weight { get; set; }
//    public double Volume { get; set; }
//    public TimeOnly Start { get; set; }
//    public TimeOnly End { get; set; }
//}
//public class RouteOptimizer
//{
//    public static List<int> SolveVRP(
//        long[,] matrixDist, long[,] matrixTime,
//        List<OptimizationNode> nodes,
//        double capKg, double capM3,
//        TimeOnly shiftStart, TimeOnly shiftEnd)
//    {
//        int count = matrixDist.GetLength(0);
//        if (count == 0) return new List<int>();

//        // 1. Chuẩn bị danh sách kết quả mặc định (nếu thuật toán lỗi thì trả về cái này)
//        // Danh sách chứa index từ 0 đến n-1 (tương ứng với nodes input)
//        var allIndices = Enumerable.Range(0, nodes.Count).ToList();

//        try
//        {
//            long horizon = (long)(shiftEnd - shiftStart).TotalMinutes;
//            RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
//            RoutingModel routing = new RoutingModel(manager);

//            // --- Cấu hình Cost (như cũ) ---
//            int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
//                matrixDist[manager.IndexToNode(i), manager.IndexToNode(j)]);
//            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

//            // --- Cấu hình Time (như cũ) ---
//            int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
//            {
//                int from = manager.IndexToNode(i);
//                int to = manager.IndexToNode(j);
//                long travel = (long)Math.Ceiling(matrixTime[from, to] / 60.0);
//                long service = (from == 0) ? 0 : 15;
//                return travel + service;
//            });

//            // Tăng Horizon lên một chút để tránh lỗi biên (slack)
//            routing.AddDimension(timeCallbackIndex, 10000, horizon + 120, false, "Time");
//            var timeDim = routing.GetMutableDimension("Time");
//            timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, horizon);

//            for (int i = 0; i < nodes.Count; i++)
//            {
//                long index = manager.NodeToIndex(i + 1);
//                var node = nodes[i];

//                // FIX: Nếu giờ không hợp lệ, cho phép phục vụ bất cứ lúc nào trong ca
//                long startMin = Math.Max(0, (long)(node.Start - shiftStart).TotalMinutes);
//                long endMin = Math.Min(horizon, (long)(node.End - shiftStart).TotalMinutes);

//                if (endMin <= startMin)
//                {
//                    startMin = 0;
//                    endMin = horizon;
//                }

//                timeDim.CumulVar(index).SetRange(startMin, endMin);

//                // QUAN TRỌNG: Penalty cực lớn để ép OR-Tools cố gắng ghé thăm node này
//                // Nếu không thể ghé thăm (bất khả thi), nó sẽ drop node này ra khỏi solution
//                routing.AddDisjunction(new long[] { index }, 10_000_000);
//            }

//            // --- Cấu hình Weight/Volume (như cũ) --- 
//            // Lưu ý: Nếu muốn ép nhận đơn quá tải, bạn có thể bỏ qua AddDimension phần này
//            // Hoặc set capacity cực lớn. Ở đây tôi giữ nguyên để nó tối ưu,
//            // nhưng các đơn thừa ra sẽ được xử lý ở bước Fallback bên dưới.
//            int weightCallback = routing.RegisterUnaryTransitCallback((long i) => {
//                int node = manager.IndexToNode(i);
//                return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
//            });
//            routing.AddDimension(weightCallback, 0, (long)(capKg * 100 * 2), true, "Weight"); // *2 capacity để nới lỏng

//            int volumeCallback = routing.RegisterUnaryTransitCallback((long i) => {
//                int node = manager.IndexToNode(i);
//                return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
//            });
//            routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000 * 2), true, "Volume"); // *2 capacity

//            // --- Solve ---
//            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
//            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
//            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 1 };

//            Assignment solution = routing.SolveWithParameters(searchParameters);

//            if (solution != null)
//            {
//                var optimizedIndices = new List<int>();
//                long index = routing.Start(0);
//                while (!routing.IsEnd(index))
//                {
//                    int node = manager.IndexToNode(index);
//                    if (node != 0) optimizedIndices.Add(node - 1);
//                    index = solution.Value(routing.NextVar(index));
//                }

//                // --- BƯỚC QUAN TRỌNG NHẤT: FALLBACK ---
//                // Tìm những node bị thuật toán bỏ qua (Dropped nodes)
//                var missingIndices = allIndices.Except(optimizedIndices).ToList();

//                // Nối những node bị thiếu vào cuối danh sách đã tối ưu
//                if (missingIndices.Any())
//                {
//                    // Console.WriteLine($"[INFO] Force adding {missingIndices.Count} dropped nodes.");
//                    optimizedIndices.AddRange(missingIndices);
//                }

//                return optimizedIndices;
//            }
//        }
//        catch (Exception ex)
//        {
//            // Log lỗi nếu cần
//            Console.WriteLine($"[OR-TOOLS Error] {ex.Message}. Using default order.");
//        }

//        // Nếu lỗi hoặc không tìm thấy giải pháp, trả về danh sách gốc (0, 1, 2...)
//        // Để đảm bảo đơn hàng vẫn hiện ra
//        return allIndices;
//    }
//}
