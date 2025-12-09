using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Services;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService
{
	public class EmguImageQualityService : IImageComparisonService
	{
		private readonly ILogger<EmguImageQualityService> _logger;
		private readonly HttpClient _httpClient;

		// Constructor
		public EmguImageQualityService(ILogger<EmguImageQualityService> logger)
		{
			_logger = logger;
			_httpClient = new HttpClient();
			// Giả lập Browser để tránh bị chặn bởi một số server (như Tiki, Shopee)
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
		}

		private async Task<Mat> LoadImageFromUrlOrPath(string input)
		{
			try
			{
				_logger.LogInformation($"Loading image from {input}");

				if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					var imageBytes = await _httpClient.GetByteArrayAsync(input);
					Mat img = new Mat();
					CvInvoke.Imdecode(imageBytes, ImreadModes.Grayscale, img);
					_logger.LogInformation("Image loaded successfully.");
					return img;
				}
				else
				{
					var img = CvInvoke.Imread(input, ImreadModes.Grayscale);
					if (img.IsEmpty)
					{
						_logger.LogWarning("Failed to load image from local path.");
					}
					return img;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error loading image: {ex.Message}");
				return new Mat();  // Trả về Mat rỗng nếu lỗi
			}
		}

		private async Task<double> ComputeSimilarityAsync(string url1, string url2)
		{
			try
			{
				using var img1 = await LoadImageFromUrlOrPath(url1);
				using var img2 = await LoadImageFromUrlOrPath(url2);

				if (img1.IsEmpty || img2.IsEmpty)
				{
					_logger.LogWarning("One of the images is empty, skipping comparison.");
					return 0;
				}

				_logger.LogInformation("Resizing images...");
				PrepareImage(img1);
				PrepareImage(img2);

				using var detector = new AKAZE();
				using var descriptors1 = new Mat();
				using var descriptors2 = new Mat();
				VectorOfKeyPoint keypoints1 = new VectorOfKeyPoint();
				VectorOfKeyPoint keypoints2 = new VectorOfKeyPoint();

				detector.DetectAndCompute(img1, null, keypoints1, descriptors1, false);
				detector.DetectAndCompute(img2, null, keypoints2, descriptors2, false);

				if (descriptors1.Rows == 0 || descriptors2.Rows == 0)
				{
					_logger.LogWarning("No descriptors found.");
					return 0;
				}

				using var matcher = new BFMatcher(DistanceType.Hamming, false);
				var matches = new VectorOfVectorOfDMatch();
				matcher.KnnMatch(descriptors1, descriptors2, matches, 2);

				var goodMatches = new List<MDMatch>();
				for (int i = 0; i < matches.Size; i++)
				{
					var match = matches[i].ToArray();
					if (match.Length >= 2 && match[0].Distance < 0.7 * match[1].Distance)
					{
						goodMatches.Add(match[0]);
					}
				}

				_logger.LogInformation($"Good matches found: {goodMatches.Count}");

				if (goodMatches.Count < 4)
				{
					_logger.LogWarning("Not enough good matches.");
					return 0;
				}

				var srcPts = goodMatches.Select(m => new PointF(keypoints1[m.QueryIdx].Point.X, keypoints1[m.QueryIdx].Point.Y)).ToArray();
				var dstPts = goodMatches.Select(m => new PointF(keypoints2[m.TrainIdx].Point.X, keypoints2[m.TrainIdx].Point.Y)).ToArray();

				if (srcPts.Length >= 4)
				{
					using var mask = new Mat();
					var homography = CvInvoke.FindHomography(srcPts, dstPts, RobustEstimationAlgorithm.Ransac, 5.0, mask);

					if (!homography.IsEmpty)
					{
						int inliers = CvInvoke.CountNonZero(mask);
						_logger.LogInformation($"Inliers count: {inliers}");
						double score = Math.Min((double)inliers / 15.0 * 100, 100);
						return Math.Round(score, 2);
					}
				}

				return Math.Min((double)goodMatches.Count / 20.0 * 100, 100);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in similarity computation: {ex.Message}");
				return 0;
			}
		}

		public async Task<bool> CompareImagesSimilarityAsync(List<string> urls1, List<string> urls2)
		{
			if (urls1 == null || !urls1.Any() || urls2 == null || !urls2.Any())
			{
				return false;
			}

			// Cách 1: Tối ưu đơn giản - Thoát ngay khi tìm thấy (Vẫn chạy tuần tự nhưng nhanh hơn nếu may mắn)
			foreach (var urlA in urls1)
			{
				foreach (var urlB in urls2)
				{
					double similarity = await ComputeSimilarityAsync(urlA, urlB);

					// Nếu tìm thấy 1 cặp giống > 80% thì return True luôn, không cần check các cặp còn lại
					if (similarity > 80)
					{
						_logger.LogInformation($"Match found between {urlA} and {urlB} with score {similarity}");
						return true;
					}
				}
			}

			return false;
		}

		private void PrepareImage(Mat img)
		{
			if (img.Width > 800)
			{
				double scale = 800.0 / img.Width;
				CvInvoke.Resize(img, img, new Size(0, 0), scale, scale);
			}
		}
	}
}
