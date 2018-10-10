using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace photobloater
{
    public class Program
    {
        public static string[] ExtensionMask = new string[]{".png", ".jpg", ".jpeg", ".bmp"};
        public static void Main(string[] args)
        {
            Console.WriteLine("- P H O T O B L O A T E R -");
           
            if (args.Length.Equals(4))
            {
                string targetPath = args[0];
                Console.WriteLine($"Target Path: {targetPath}");
                
                int minWidth = Convert.ToInt32(args[1]);
                Console.WriteLine($"Min Width: {minWidth} px");

                int minHeight = Convert.ToInt32(args[2]);
                Console.WriteLine($"Min Height: {minHeight} px");

                int resizeFactor = Convert.ToInt32(args[3]);
                Console.WriteLine($"Resize Factor: {resizeFactor}");

                Console.WriteLine("Press any key to begin...");
                Console.ReadLine();
                
                ConcurrentBag<PhotoInfo> collectedPhotos = CollectPhotos(targetPath, minWidth, minHeight);

                Console.WriteLine($"{collectedPhotos.Count} photos found. Press any key to bloat...");
                Console.ReadLine();

                BloatPhotos(collectedPhotos, resizeFactor);
            }
            else
            {
                Console.WriteLine($"Usage: photobloater.exe target_path min_width min_height");
            }
        }

        public static ConcurrentBag<PhotoInfo> CollectPhotos(string targetPath, int minWidth, int minHeight)
        {
            ConcurrentBag<PhotoInfo> collectedPhotos = new ConcurrentBag<PhotoInfo>();

            IEnumerable<string> filteredFilePaths = Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories)
                .Where(f => ExtensionMask.Contains(Path.GetExtension(f)));

            try
            {            
                Parallel.ForEach (filteredFilePaths, (currentFile) =>
                {
                    using (Image<Rgba32> image = Image.Load(currentFile))
                    {
                        if (image.Width <= minWidth || image.Height <= minHeight)
                        {
                            Console.WriteLine($"Found Photo: {currentFile} ({image.Width}x{image.Height})");
                            
                            FileInfo file = new FileInfo(currentFile);

                            PhotoInfo info = new PhotoInfo(){ImageData = image, FileData = file};

                            collectedPhotos.Add(info);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return collectedPhotos;
        }

        public static void BloatPhotos(ConcurrentBag<PhotoInfo> collectedPhotos, int resizeFactor)
        {
            try
            {
                Parallel.ForEach(collectedPhotos, (currentPhoto) => 
                {
                    using (currentPhoto.ImageData = Image.Load(currentPhoto.FileData.FullName))
                    {
                        Console.WriteLine($"Bloating photo: {currentPhoto.FileData.FullName} "
                        + $"({currentPhoto.ImageData.Width}x{currentPhoto.ImageData.Height}) -> "
                        + $"({currentPhoto.ImageData.Width * resizeFactor}x{currentPhoto.ImageData.Height * resizeFactor})");
                    
                        currentPhoto.ImageData.Mutate(i => i
                            .Resize(currentPhoto.ImageData.Width * resizeFactor, currentPhoto.ImageData.Height * resizeFactor));
                    
                        currentPhoto.ImageData.Save(currentPhoto.FileData.FullName);
                    }
                });   
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class PhotoInfo
    {
        public FileInfo FileData { get; set;}
        public Image<Rgba32> ImageData {get; set;}
    }
}
