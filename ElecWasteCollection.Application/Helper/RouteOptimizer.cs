using ElecWasteCollection.Application.Model.GroupModel;
using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;

namespace ElecWasteCollection.Application.Helpers
{

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

            var allIndices = Enumerable.Range(0, nodes.Count).ToList();

            try
            {
                RoutingIndexManager manager = new RoutingIndexManager(count, 1, 0);
                RoutingModel routing = new RoutingModel(manager);

                // Giảm thiểu tổng mét đường xe chạy -> Tiết kiệm xăng
                int transitCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                {
                    var from = manager.IndexToNode(i);
                    var to = manager.IndexToNode(j);
                    return matrixDist[from, to];
                });
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);


                // TIME WINDOWS
                int timeCallbackIndex = routing.RegisterTransitCallback((long i, long j) =>
                {
                    int from = manager.IndexToNode(i);
                    int to = manager.IndexToNode(j);
                    long travel = (long)Math.Ceiling(matrixTime[from, to] / 60.0);
                    long service = (from == 0) ? 0 : 15;
                    return travel + service;
                });
                long shiftDuration = (long)(shiftEnd - shiftStart).TotalMinutes;
                long horizon = shiftDuration;

                routing.AddDimension(
                    timeCallbackIndex,
                    10000,  
                    horizon, 
                    false,  
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

                    if (endMin <= startMin) { startMin = 0; endMin = horizon; }

                    // Không được đến SỚM hơn giờ mở cửa
                    timeDim.CumulVar(index).SetMin(startMin);

                    // NÊN đến trước giờ đóng cửa
                    timeDim.SetCumulVarSoftUpperBound(index, endMin, 2000);

                    // KHÔNG ĐƯỢC BỎ ĐƠN
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

                // Chọn cung đường rẻ nhất để khởi tạo
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

                // tối ưu xăng
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;

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
                Console.WriteLine($"[OR-TOOLS Error] {ex.Message}");
            }

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
        public List<PreAssignProduct> Tag { get; set; } = new List<PreAssignProduct>();
    }
}
