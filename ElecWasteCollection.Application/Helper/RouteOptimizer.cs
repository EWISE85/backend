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

            // Tính tổng thời gian ca làm việc (Horizon)
            long horizon = (long)(shiftEnd - shiftStart).TotalMinutes;

            Console.WriteLine($"\n[OR-TOOLS] Bắt đầu tính toán VRP cho {nodes.Count} điểm.");
            Console.WriteLine($"[OR-TOOLS] Ca: {shiftStart}-{shiftEnd} | Horizon: {horizon} phút");

            RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
            RoutingModel routing = new RoutingModel(manager);

            // 1. Chi phí = Khoảng cách
            int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                matrixDist[manager.IndexToNode(i), manager.IndexToNode(j)]);
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // 2. Ràng buộc Thời gian (Time Dimension)
            int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
            {
                int fromNode = manager.IndexToNode(i);
                int toNode = manager.IndexToNode(j);

                // FIX: Mapbox trả về giây -> Đổi ra phút và làm tròn lên (tránh = 0)
                long seconds = matrixTime[fromNode, toNode];
                long travelTimeMin = (long)Math.Ceiling(seconds / 60.0);

                // Thời gian phục vụ: Depot = 0, Khách = 15p
                long serviceTime = (fromNode == 0) ? 0 : 15;

                return travelTimeMin + serviceTime;
            });

            // Slack 120p (cho phép chờ), Horizon = độ dài ca
            routing.AddDimension(timeCallbackIndex, 120, horizon, false, "Time");
            var timeDim = routing.GetMutableDimension("Time");

            // Set khung giờ Depot (0 -> Horizon)
            timeDim.CumulVar(manager.NodeToIndex(0)).SetRange(0, horizon);

            // Set khung giờ cho các Node
            for (int i = 0; i < nodes.Count; i++)
            {
                long index = manager.NodeToIndex(i + 1);
                var node = nodes[i];

                long startMin = (long)(node.Start - shiftStart).TotalMinutes;
                long endMin = (long)(node.End - shiftStart).TotalMinutes;

                // FIX: Đảm bảo không âm (do lệch Timezone hoặc data lỗi)
                long effectiveStart = Math.Max(0, startMin);
                long effectiveEnd = Math.Max(0, endMin);

                // FIX QUAN TRỌNG: Không được vượt quá Horizon của ca làm việc
                if (effectiveEnd > horizon) effectiveEnd = horizon;

                // FIX LỖI SET RANGE: Nếu sau khi cắt gọt mà Start > End -> Mở rộng End ra chút
                if (effectiveEnd < effectiveStart) effectiveEnd = effectiveStart + 15;

                // Kiểm tra lần cuối: Nếu Start vượt quá Horizon -> Node này không thể phục vụ
                if (effectiveStart > horizon)
                {
                    Console.WriteLine($"[CẢNH BÁO] Node {i + 1} nằm ngoài giờ ca làm việc -> Sẽ bị bỏ qua.");
                    // Gán penalty cực lớn để Solver bỏ qua node này thay vì báo lỗi Infeasible
                    routing.AddDisjunction(new long[] { index }, 10000000);
                    continue;
                }

                // Set Range an toàn
                timeDim.CumulVar(index).SetRange(effectiveStart, effectiveEnd);

                // Cho phép bỏ qua điểm này nếu không thể xếp lịch (Penalty cao)
                // Điều này giúp thuật toán trả về các điểm đi được thay vì trả về rỗng
                routing.AddDisjunction(new long[] { index }, 1000000);
            }

            // 3. Ràng buộc Tải trọng
            int weightCallback = routing.RegisterUnaryTransitCallback((long i) => {
                int node = manager.IndexToNode(i);
                return node == 0 ? 0 : (long)(nodes[node - 1].Weight * 100);
            });
            routing.AddDimension(weightCallback, 0, (long)(capKg * 100), true, "Weight");

            // 4. Ràng buộc Thể tích
            int volumeCallback = routing.RegisterUnaryTransitCallback((long i) => {
                int node = manager.IndexToNode(i);
                return node == 0 ? 0 : (long)(nodes[node - 1].Volume * 10000);
            });
            routing.AddDimension(volumeCallback, 0, (long)(capM3 * 10000), true, "Volume");

            // 5. Cấu hình Search
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 2 }; // Tăng lên 2s cho chắc

            Assignment solution = routing.SolveWithParameters(searchParameters);

            var res = new List<int>();
            if (solution != null)
            {
                Console.WriteLine("[OR-TOOLS] --> Success!");
                long index = routing.Start(0);
                while (!routing.IsEnd(index))
                {
                    int node = manager.IndexToNode(index);
                    if (node != 0) res.Add(node - 1); // Trả về index gốc của list nodes
                    index = solution.Value(routing.NextVar(index));
                }
            }
            else
            {
                Console.WriteLine("[OR-TOOLS] --> Failed / Infeasible.");
                // Fallback: Trả về danh sách gốc để không mất đơn (tùy logic bạn chọn)
                // return Enumerable.Range(0, nodes.Count).ToList(); 
            }
            return res;
        }
    }
}