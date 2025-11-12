//using ElecWasteCollection.Application.IServices;
//using OpenCvSharp;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ElecWasteCollection.Application.Services
//{
//	public class ImageComparisonService : IImageComparisonService
//	{
//		public double CompareImageSimilarity(string imagePath1, string imagePath2)
//		{
//			try
//			{
//				// 1. Đọc ảnh và chuyển sang đen trắng (Grayscale) để thuật toán chạy chuẩn hơn
//				using var img1 = Cv2.ImRead(imagePath1, ImreadModes.Grayscale);
//				using var img2 = Cv2.ImRead(imagePath2, ImreadModes.Grayscale);

//				if (img1.Empty() || img2.Empty())
//					throw new Exception("Không thể đọc file ảnh.");

//				// 2. Khởi tạo thuật toán ORB (Oriented FAST and Rotated BRIEF)
//				// ORB nhanh và hiệu quả cho việc so sánh vật thể
//				using var orb = ORB.Create(500); // Tìm tối đa 500 điểm đặc trưng

//				// 3. Tìm Keypoints và Descriptors
//				// Keypoints: Vị trí các điểm đặc biệt (góc, cạnh, nút bấm...)
//				// Descriptors: Dữ liệu mô tả các điểm đó
//				using var descriptors1 = new Mat();
//				using var descriptors2 = new Mat();
//				KeyPoint[] keypoints1, keypoints2;

//				orb.DetectAndCompute(img1, null, out keypoints1, descriptors1);
//				orb.DetectAndCompute(img2, null, out keypoints2, descriptors2);

//				// Nếu không tìm thấy điểm đặc trưng nào (ảnh quá mờ hoặc trơn tuột), trả về 0
//				if (descriptors1.Rows == 0 || descriptors2.Rows == 0)
//					return 0;

//				// 4. So sánh các điểm đặc trưng (Matching)
//				// Dùng BFMatcher (Brute Force) với chuẩn Hamming (phù hợp cho ORB)
//				using var matcher = new BFMatcher(NormTypes.Hamming, crossCheck: false);

//				// Tìm 2 điểm khớp tốt nhất cho mỗi điểm (k = 2) để lọc nhiễu
//				var matches = matcher.KnnMatch(descriptors1, descriptors2, k: 2);

//				// 5. Lọc kết quả tốt (Lowe's Ratio Test)
//				// Chỉ lấy các cặp điểm khớp thực sự tốt, loại bỏ các điểm khớp ngẫu nhiên
//				var goodMatches = new List<DMatch>();
//				foreach (var match in matches)
//				{
//					if (match.Length >= 2 && match[0].Distance < 0.75 * match[1].Distance)
//					{
//						goodMatches.Add(match[0]);
//					}
//				}

//				// 6. Tính điểm số (Similarity Score)
//				// Logic: Số lượng điểm khớp tốt / Tổng số điểm đặc trưng tìm được
//				// Đây là công thức heuristic đơn giản
//				double ratio = (double)goodMatches.Count / keypoints1.Length;

//				// Chuẩn hóa thành thang 0-100. 
//				// Lưu ý: Với máy giặt, match được khoảng 10-15% số điểm đã là rất cao rồi (vì góc chụp khác nhau).
//				// Ta nhân hệ số để con số thân thiện hơn với người dùng.
//				double score = Math.Min(ratio * 100 * 3, 100); // Nhân 3 để scale lên (tùy chỉnh ngưỡng này khi test thực tế)

//				return Math.Round(score, 2);
//			}
//			catch (Exception ex)
//			{
//				Console.WriteLine($"Lỗi so sánh ảnh: {ex.Message}");
//				return 0;
//			}
//		}
//	}
//}
