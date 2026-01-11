using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;

namespace ElecWasteCollection.Application.Helpers
{
    public class OptimizationNode
    {
        public int OriginalIndex { get; set; }
        public double Weight { get; set; }
        public double Volume { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
    }
    public class RouteOptimizer
    {
        public static List<int> SolveVRP(
            long[,] matrixDist, long[,] matrixTime,
            List<OptimizationNode> nodes,
            double capKg, double capM3,
            TimeOnly shiftStart, TimeOnly shiftEnd)
        {
            int count = matrixDist.GetLength(0);
            if (count == 0) return new List<int>();

            // 1. Chuẩn bị danh sách kết quả mặc định (nếu thuật toán lỗi thì trả về cái này)
            // Danh sách chứa index từ 0 đến n-1 (tương ứng với nodes input)
            var allIndices = Enumerable.Range(0, nodes.Count).ToList();

            try
            {
                long horizon = (long)(shiftEnd - shiftStart).TotalMinutes;
                RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
                RoutingModel routing = new RoutingModel(manager);

                // --- Cấu hình Cost (như cũ) ---
                int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                    matrixDist[manager.IndexToNode(i), manager.IndexToNode(j)]);
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // --- Cấu hình Time (như cũ) ---
                int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                {
                    int from = manager.IndexToNode(i);
                    int to = manager.IndexToNode(j);
                    long travel = (long)Math.Ceiling(matrixTime[from, to] / 60.0);
                    long service = (from == 0) ? 0 : 15;
                    return travel + service;
                });

                // Tăng Horizon lên một chút để tránh lỗi biên (slack)
                routing.AddDimension(timeCallbackIndex, 10000, horizon + 120, false, "Time");
                var timeDim = routing.GetMutableDimension("Time");
                timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, horizon);

                for (int i = 0; i < nodes.Count; i++)
                {
                    long index = manager.NodeToIndex(i + 1);
                    var node = nodes[i];

                    // FIX: Nếu giờ không hợp lệ, cho phép phục vụ bất cứ lúc nào trong ca
                    long startMin = Math.Max(0, (long)(node.Start - shiftStart).TotalMinutes);
                    long endMin = Math.Min(horizon, (long)(node.End - shiftStart).TotalMinutes);

                    if (endMin <= startMin)
                    {
                        startMin = 0;
                        endMin = horizon;
                    }

                    timeDim.CumulVar(index).SetRange(startMin, endMin);

                    // QUAN TRỌNG: Penalty cực lớn để ép OR-Tools cố gắng ghé thăm node này
                    // Nếu không thể ghé thăm (bất khả thi), nó sẽ drop node này ra khỏi solution
                    routing.AddDisjunction(new long[] { index }, 10_000_000);
                }

                // --- Cấu hình Weight/Volume (như cũ) --- 
                // Lưu ý: Nếu muốn ép nhận đơn quá tải, bạn có thể bỏ qua AddDimension phần này
                // Hoặc set capacity cực lớn. Ở đây tôi giữ nguyên để nó tối ưu,
                // nhưng các đơn thừa ra sẽ được xử lý ở bước Fallback bên dưới.
                int weightCallback = routing.RegisterUnaryTransitCallback((long i) => {
                    int node = manager.IndexToNode(i);
                    return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
                });
                routing.AddDimension(weightCallback, 0, (long)(capKg * 100 * 2), true, "Weight"); // *2 capacity để nới lỏng

                int volumeCallback = routing.RegisterUnaryTransitCallback((long i) => {
                    int node = manager.IndexToNode(i);
                    return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
                });
                routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000 * 2), true, "Volume"); // *2 capacity

                // --- Solve ---
                RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 1 };

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

                    // --- BƯỚC QUAN TRỌNG NHẤT: FALLBACK ---
                    // Tìm những node bị thuật toán bỏ qua (Dropped nodes)
                    var missingIndices = allIndices.Except(optimizedIndices).ToList();

                    // Nối những node bị thiếu vào cuối danh sách đã tối ưu
                    if (missingIndices.Any())
                    {
                        // Console.WriteLine($"[INFO] Force adding {missingIndices.Count} dropped nodes.");
                        optimizedIndices.AddRange(missingIndices);
                    }

                    return optimizedIndices;
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"[OR-TOOLS Error] {ex.Message}. Using default order.");
            }

            // Nếu lỗi hoặc không tìm thấy giải pháp, trả về danh sách gốc (0, 1, 2...)
            // Để đảm bảo đơn hàng vẫn hiện ra
            return allIndices;
        }
    }

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

    //        // Tính tổng thời gian ca làm việc (Horizon)
    //        long horizon = (long)(shiftEnd - shiftStart).TotalMinutes;

    //        Console.WriteLine($"\n[OR-TOOLS] Bắt đầu tính toán VRP cho {nodes.Count} điểm.");
    //        Console.WriteLine($"[OR-TOOLS] Ca: {shiftStart}-{shiftEnd} | Horizon: {horizon} phút");

    //        RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
    //        RoutingModel routing = new RoutingModel(manager);

    //        // 1. Chi phí = Khoảng cách
    //        int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
    //            matrixDist[manager.IndexToNode(i), manager.IndexToNode(j)]);
    //        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

    //        // 2. Ràng buộc Thời gian (Time Dimension)
    //        int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
    //        {
    //            int fromNode = manager.IndexToNode(i);
    //            int toNode = manager.IndexToNode(j);

    //            // FIX: Mapbox trả về giây -> Đổi ra phút và làm tròn lên (tránh = 0)
    //            long seconds = matrixTime[fromNode, toNode];
    //            long travelTimeMin = (long)Math.Ceiling(seconds / 60.0);

    //            // Thời gian phục vụ: Depot = 0, Khách = 15p
    //            long serviceTime = (fromNode == 0) ? 0 : 15;

    //            return travelTimeMin + serviceTime;
    //        });

    //        // Slack 120p (cho phép chờ), Horizon = độ dài ca
    //        routing.AddDimension(timeCallbackIndex, 120, horizon, false, "Time");
    //        var timeDim = routing.GetMutableDimension("Time");

    //        // Set khung giờ Depot (0 -> Horizon)
    //        timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, horizon);

    //        // Set khung giờ cho các Node
    //        for (int i = 0; i < nodes.Count; i++)
    //        {
    //            long index = manager.NodeToIndex(i + 1);
    //            var node = nodes[i];

    //            long startMin = (long)(node.Start - shiftStart).TotalMinutes;
    //            long endMin = (long)(node.End - shiftStart).TotalMinutes;

    //            // FIX: Đảm bảo không âm (do lệch Timezone hoặc data lỗi)
    //            long effectiveStart = Math.Max(0, startMin);
    //            long effectiveEnd = Math.Max(0, endMin);

    //            // FIX QUAN TRỌNG: Không được vượt quá Horizon của ca làm việc
    //            if (effectiveEnd > horizon) effectiveEnd = horizon;

    //            // FIX LỖI SET RANGE: Nếu sau khi cắt gọt mà Start > End -> Mở rộng End ra chút
    //            if (effectiveEnd < effectiveStart) effectiveEnd = effectiveStart + 15;

    //            // Kiểm tra lần cuối: Nếu Start vượt quá Horizon -> Node này không thể phục vụ
    //            if (effectiveStart > horizon)
    //            {
    //                Console.WriteLine($"[CẢNH BÁO] Node {i + 1} nằm ngoài giờ ca làm việc -> Sẽ bị bỏ qua.");
    //                // Gán penalty cực lớn để Solver bỏ qua node này thay vì báo lỗi Infeasible
    //                routing.AddDisjunction(new long[] { index }, 10000000);
    //                continue;
    //            }

    //            // Set Range an toàn
    //            timeDim.CumulVar(index).SetRange(effectiveStart, effectiveEnd);

    //            // Cho phép bỏ qua điểm này nếu không thể xếp lịch (Penalty cao)
    //            // Điều này giúp thuật toán trả về các điểm đi được thay vì trả về rỗng
    //            routing.AddDisjunction(new long[] { index }, 1000000);
    //        }

    //        // 3. Ràng buộc Tải trọng
    //        int weightCallback = routing.RegisterUnaryTransitCallback((long i) => {
    //            int node = manager.IndexToNode(i);
    //            return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
    //        });
    //        routing.AddDimension(weightCallback, 0, (long)(capKg * 100), true, "Weight");

    //        // 4. Ràng buộc Thể tích
    //        int volumeCallback = routing.RegisterUnaryTransitCallback((long i) => {
    //            int node = manager.IndexToNode(i);
    //            return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
    //        });
    //        routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000), true, "Volume");

    //        // 5. Cấu hình Search
    //        RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
    //        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
    //        searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 2 }; // Tăng lên 2s cho chắc

    //        Assignment solution = routing.SolveWithParameters(searchParameters);

    //        var res = new List<int>();
    //        if (solution != null)
    //        {
    //            Console.WriteLine("[OR-TOOLS] --> Success!");
    //            long index = routing.Start(0);
    //            while (!routing.IsEnd(index))
    //            {
    //                int node = manager.IndexToNode(index);
    //                if (node != 0) res.Add(node - 1); // Trả về index gốc của list nodes
    //                index = solution.Value(routing.NextVar(index));
    //            }
    //        }
    //        else
    //        {
    //            Console.WriteLine("[OR-TOOLS] --> Failed / Infeasible.");
    //            // Fallback: Trả về danh sách gốc để không mất đơn (tùy logic bạn chọn)
    //            // return Enumerable.Range(0, nodes.Count).ToList(); 
    //        }
    //        return res;
    //    }
    //}
}