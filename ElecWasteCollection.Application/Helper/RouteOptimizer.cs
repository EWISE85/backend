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

            Console.WriteLine($"\n[OR-TOOLS] Bắt đầu tính toán cho {nodes.Count} điểm giao hàng.");
            Console.WriteLine($"[OR-TOOLS] Ca làm việc: {shiftStart} - {shiftEnd} (Tổng: {(shiftEnd - shiftStart).TotalMinutes} phút)");

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                double startOffset = (n.Start - shiftStart).TotalMinutes;
                double endOffset = (n.End - shiftStart).TotalMinutes;
                Console.WriteLine($"  + Node {i + 1}: Yêu cầu {n.Start}-{n.End} | Offset: {startOffset}p -> {endOffset}p | Nặng: {n.Weight}kg");

                if (endOffset < 0)
                    Console.WriteLine("CẢNH BÁO: Khách hẹn giờ trước khi ca làm việc bắt đầu!");
            }

            RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
            RoutingModel routing = new RoutingModel(manager);

            // 1. Chi phí = Khoảng cách
            int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                matrixDist[manager.IndexToNode(i), manager.IndexToNode(j)]);
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // 2. Ràng buộc Thời gian (QUAN TRỌNG)
            int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
            {
                int fromNode = manager.IndexToNode(i);
                int toNode = manager.IndexToNode(j);

                // Mapbox trả về giây -> Đổi ra phút
                long travelTimeMin = matrixTime[fromNode, toNode] / 60;

                // Thời gian phục vụ (bốc hàng)
                // Depot (0) = 0 phút, Khách = 15 phút
                long serviceTime = (fromNode == 0) ? 0 : 15;

                return travelTimeMin + serviceTime;
            });

            // Slack (thời gian chờ tối đa): Cho phép chờ tới 120 phút (2 tiếng) để dễ tìm đường hơn
            // Capacity: Tổng thời gian ca làm việc
            routing.AddDimension(timeCallbackIndex, 120, (long)(shiftEnd - shiftStart).TotalMinutes, false, "Time");
            var timeDim = routing.GetMutableDimension("Time");

            // Set khung giờ cho Depot (Toàn bộ ca)
            timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, (long)(shiftEnd - shiftStart).TotalMinutes);

            // Set khung giờ cho Khách hàng
            for (int i = 0; i < nodes.Count; i++)
            {
                long index = manager.NodeToIndex(i + 1);
                long startMin = (long)(nodes[i].Start - shiftStart).TotalMinutes;
                long endMin = (long)(nodes[i].End - shiftStart).TotalMinutes;
                long effectiveStart = Math.Max(0, startMin);
                long effectiveEnd = Math.Max(0, endMin);

                if (effectiveEnd < effectiveStart) effectiveEnd = effectiveStart + 15; 

                timeDim.CumulVar(index).SetRange(effectiveStart, effectiveEnd);
            }

            // 3. Ràng buộc Tải trọng (Weight)
            int weightCallback = routing.RegisterUnaryTransitCallback((long i) => {
                int node = manager.IndexToNode(i);
                return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
            });
            routing.AddDimension(weightCallback, 0, (long)(capKg * 100), true, "Weight");

            // 4. Ràng buộc Thể tích (Volume)
            int volumeCallback = routing.RegisterUnaryTransitCallback((long i) => {
                int node = manager.IndexToNode(i);
                return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
            });
            routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000), true, "Volume");

            // 5. Cấu hình tìm kiếm
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            // Giới hạn thời gian tìm kiếm để không bị treo (ví dụ 1 giây)
            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 1 };

            Assignment solution = routing.SolveWithParameters(searchParameters);

            var res = new List<int>();
            if (solution != null)
            {
                Console.WriteLine("[OR-TOOLS] TÌM THẤY LỘ TRÌNH! (Status: Success)");
                long index = routing.Start(0);
                while (!routing.IsEnd(index))
                {
                    int node = manager.IndexToNode(index);
                    if (node != 0) res.Add(node - 1);
                    index = solution.Value(routing.NextVar(index));
                }
            }
            else
            {
                Console.WriteLine("[OR-TOOLS]  KHÔNG TÌM THẤY LỘ TRÌNH (Status: Fail/Infeasible)");
                Console.WriteLine("Lý do có thể: Time Window quá chặt hoặc Mapbox tính thời gian đi quá lâu.");
            }
            return res;
        }
    }
}