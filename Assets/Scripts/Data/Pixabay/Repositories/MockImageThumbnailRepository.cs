using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Domain.Repositories;
using UnityEngine;

namespace ImageSearch.Data.Pixabay.Repositories
{
    public sealed class MockImageThumbnailRepository : IImageThumbnailRepository
    {
        public UniTask<Result<byte[], NetworkError>> LoadAsync(Uri thumbnailUrl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var texture = new Texture2D(96, 72, TextureFormat.RGBA32, false);
            var seed = Math.Abs(thumbnailUrl.AbsolutePath.GetHashCode()) % 6;
            var backgrounds = new[] { new Color32(188,216,196,255), new Color32(168,217,212,255), new Color32(240,199,198,255), new Color32(234,203,159,255), new Color32(174,185,216,255), new Color32(215,183,161,255) };
            var hills = new[] { new Color32(70,121,93,255), new Color32(40,124,131,255), new Color32(168,95,121,255), new Color32(169,104,76,255), new Color32(70,87,127,255), new Color32(118,82,71,255) };
            var pixels = new Color32[96 * 72];
            for (var y = 0; y < 72; y++)
                for (var x = 0; x < 96; x++)
                {
                    var dx = x - 72; var dy = y - 52;
                    var isSun = dx * dx + dy * dy < 80;
                    var backHill = (x - 30) * (x - 30) / 2 + (y - 25) * (y - 25) < 520;
                    var frontHill = (x - 72) * (x - 72) / 2 + (y - 18) * (y - 18) < 650;
                    pixels[y * 96 + x] = isSun ? new Color32(247,230,167,255) : frontHill ? hills[seed] : backHill ? new Color32(109,154,120,255) : backgrounds[seed];
                }
            texture.SetPixels32(pixels); texture.Apply(false, false);
            var png = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);
            return UniTask.FromResult(Result<byte[], NetworkError>.FromSuccess(png));
        }
    }
}
