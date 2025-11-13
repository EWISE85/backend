using ElecWasteCollection.Application.IServices;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ImageComparisonService : IImageComparisonService
	{
		private readonly HttpClient _httpClient;

		public ImageComparisonService()
		{
			_httpClient = new HttpClient();
			// Giả lập Browser để tránh bị chặn bởi một số server (như Tiki, Shopee)
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
		}

		private async Task<Mat> LoadImageFromUrlOrPath(string input)
		{
			try
			{
				// Trường hợp 1: Input là URL (http/https)
				if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					var imageBytes = await _httpClient.GetByteArrayAsync(input);
					// ImDecode đọc byte array thành ảnh. Flag Grayscale để so sánh cho chuẩn.
					return Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);
				}
				// Trường hợp 2: Input là đường dẫn file local
				else
				{
					return Cv2.ImRead(input, ImreadModes.Grayscale);
				}
			}
			catch (Exception)
			{
				return new Mat(); // Trả về Mat rỗng nếu lỗi
			}
		}

		public async Task<double> CompareImageSimilarityAsync(string url1, string url2)
		{
			try
			{
				using var img1 = await LoadImageFromUrlOrPath(url1);
				using var img2 = await LoadImageFromUrlOrPath(url2);

				if (img1.Empty() || img2.Empty()) return 0;

				// 1. Resize ảnh nếu quá lớn để khử nhiễu và tăng tốc độ
				PrepareImage(img1);
				PrepareImage(img2);

				// 2. Dùng AKAZE thay vì ORB
				// AKAZE tốt hơn cho các vật thể có bề mặt trơn/bóng và thay đổi góc chụp
				using var detector = AKAZE.Create();

				using var descriptors1 = new Mat();
				using var descriptors2 = new Mat();
				KeyPoint[] keypoints1, keypoints2;

				detector.DetectAndCompute(img1, null, out keypoints1, descriptors1);
				detector.DetectAndCompute(img2, null, out keypoints2, descriptors2);

				if (descriptors1.Rows == 0 || descriptors2.Rows == 0) return 0;

				// 3. Matching
				using var matcher = new BFMatcher(NormTypes.Hamming); // AKAZE cũng dùng binary descriptor nên dùng Hamming
				var matches = matcher.KnnMatch(descriptors1, descriptors2, k: 2);

				// 4. Lọc nhiễu cực mạnh (Strict Filter)
				var goodMatches = new List<DMatch>();
				foreach (var match in matches)
				{
					// Giảm tỉ lệ xuống 0.7 để chỉ lấy những điểm cực giống nhau
					if (match.Length >= 2 && match[0].Distance < 0.7 * match[1].Distance)
					{
						goodMatches.Add(match[0]);
					}
				}

				Console.WriteLine($"Số điểm khớp tìm thấy: {goodMatches.Count}");

				// 5. ĐÁNH GIÁ KẾT QUẢ (LOGIC MỚI)

				// Nếu tìm thấy dưới 4 điểm => Chắc chắn khác nhau
				if (goodMatches.Count < 4) return 0;

				// Dùng Homography để kiểm tra xem các điểm khớp có tạo thành hình thù đúng logic không
				// (Ví dụ: 3 cái nút thẳng hàng ở ảnh 1 thì sang ảnh 2 nó cũng phải thẳng hàng, dù bị nghiêng)
				var srcPts = goodMatches.Select(m => keypoints1[m.QueryIdx].Pt).ToArray();
				var dstPts = goodMatches.Select(m => keypoints2[m.TrainIdx].Pt).ToArray();

				// Tìm ma trận biến đổi (Chỉ chạy nếu có đủ điểm)
				if (srcPts.Length >= 4)
				{
					using var mask = new Mat();
					// RANSAC sẽ loại bỏ các điểm khớp "ảo" (outliers)
					var homography = Cv2.FindHomography(InputArray.Create(srcPts), InputArray.Create(dstPts), HomographyMethods.Ransac, 5.0, mask);

					if (!homography.Empty())
					{
						// Đếm số lượng inliers (điểm khớp đúng quy luật hình học)
						int inliers = Cv2.CountNonZero(mask);
						Console.WriteLine($"Số điểm khớp đúng hình học (Inliers): {inliers}");

						// LOGIC ĐIỂM SỐ:
						// Với vật thể 3D xoay góc, chỉ cần khoảng 10-15 inliers là xác nhận GIỐNG NHAU.
						// Ta map số lượng này sang thang 100.

						double score = Math.Min((double)inliers / 15.0 * 100, 100);
						return Math.Round(score, 2);
					}
				}

				// Fallback nếu không tìm ra homography (ít xảy ra)
				return Math.Min((double)goodMatches.Count / 20.0 * 100, 100);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 0;
			}
		}

		private void PrepareImage(Mat img)
		{
			// Resize về chiều rộng 800px để chuẩn hóa
			if (img.Width > 800)
			{
				double scale = 800.0 / img.Width;
				Cv2.Resize(img, img, new Size(0, 0), scale, scale);
			}
		}
	}
}
