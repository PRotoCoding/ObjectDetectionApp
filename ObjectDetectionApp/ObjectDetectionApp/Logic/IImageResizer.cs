using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionApp.Logic
{
    public interface IImageResizer : ICrossPlatform
    {
      Task<byte[]> ResizeImage(byte[] imageData, float width, float height);
    }
}
