using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using Functional.Tasks;
using Pipeline;
using Microsoft.FSharp.Core;

namespace FaceDetection
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ImagesList.ItemsSource = Images;
        }

        private const string ImagesFolder = "Images";

        public ObservableCollection<BitmapImage> Images = new ObservableCollection<BitmapImage>();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var fileName in Directory.GetFiles(ImagesFolder))
            {
                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    Images.Add(stream.BitmapImageFromStream());
                }
            }
        }


        // TODO : 2.5
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Images.Clear();
            // Sequential
           // StartFaceDetection(ImagesFolder);

            // Tasks
            //StartFaceDetection_Tasks(ImagesFolder);
            // Parallel using Tasks
            StartFaceDetection_Parallel_Tasks(ImagesFolder);
            //StartFaceDetection_Pipeline(ImagesFolder);
            //StartFaceDetection_Pipeline_FSharpFunc(ImagesFolder);
        }

        // Face Detection function in C#
        Bitmap DetectFaces_Sequenatial(string fileName)
        {
            var imageFrame = new Image<Bgr, byte>(fileName);
            var cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            var grayframe = imageFrame.Convert<Gray, byte>();

            var faces = cascadeClassifier.DetectMultiScale(
              grayframe, 1.1, 3, System.Drawing.Size.Empty);
            foreach (var face in faces)
                imageFrame.Draw(face, new Bgr(System.Drawing.Color.DarkRed), 3);

            return imageFrame.ToBitmap();
        }

        void StartFaceDetection(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);
            foreach (string filePath in filePaths)
            {
                var bitmap = DetectFaces_Sequenatial(filePath);
                var bitmapImage = bitmap.ToBitmapImage();
                Images.Add(bitmapImage);
            }
        }

        // Parallel implementation of the detect faces program using Tasks
        void StartFaceDetection_Tasks(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);

            var bitmaps = from filePath in filePaths
                          select Task.Run<Bitmap>(() => DetectFaces_Sequenatial(filePath));

            foreach (var bitmap in bitmaps)
            {
                var bitmapImage = bitmap.Result;
                Images.Add(bitmapImage.ToBitmapImage());
            }
        }

        //  Correct Task Parallel implantation of the Detect Faces function
        ThreadLocal<CascadeClassifier> CascadeClassifierThreadLocal =
            new ThreadLocal<CascadeClassifier>(() => new CascadeClassifier("haarcascade_frontalface_alt_tree.xml"));

        Bitmap DetectFaces_ParallelUsingTasks(string fileName)
        {
            var imageFrame = new Image<Bgr, byte>(fileName);
            var cascadeClassifier = CascadeClassifierThreadLocal.Value;
            var grayframe = imageFrame.Convert<Gray, byte>();

            var faces = cascadeClassifier.DetectMultiScale(
                grayframe, 1.1, 3, System.Drawing.Size.Empty);
            foreach (var face in faces)
                imageFrame.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
            return imageFrame.ToBitmap();
        }

        void StartFaceDetection_Parallel_Tasks(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);
            var bitmapTasks =
                (from filePath in filePaths
                 select Task.Run<Bitmap>(() => DetectFaces_ParallelUsingTasks(filePath))).ToList();

            foreach (var bitmapTask in bitmapTasks)
                bitmapTask.ContinueWith(bitmap =>
                {
                    var bitmapImage = bitmap.Result;
                    Images.Add(bitmapImage.ToBitmapImage());
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        // DetectFaces function using Task-Continuation
        Task<Bitmap> DetectFaces_TaskContinuation(string fileName)
        {
            var imageTask = Task.Run<Image<Bgr, byte>>(
                () => new Image<Bgr, byte>(fileName)
            );
            var imageFrameTask = imageTask.ContinueWith(
                image => image.Result.Convert<Gray, byte>()
            );
            var grayframeTask = imageFrameTask.ContinueWith(
               imageFrame => imageFrame.Result.Convert<Gray, byte>()
            );

            var facesTask = grayframeTask.ContinueWith(grayFrame =>
                {
                    var cascadeClassifier = CascadeClassifierThreadLocal.Value;
                    return cascadeClassifier.DetectMultiScale(
                        grayFrame.Result, 1.1, 3, System.Drawing.Size.Empty);
                }
            );

            var bitmapTask = facesTask.ContinueWith(faces =>
                {
                    foreach (var face in faces.Result)
                        imageTask.Result.Draw(
                              face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return imageTask.Result.ToBitmap();
                }
            );
            return bitmapTask;
        }

        // TODO 2.7
        // Detect Faces function using Task-Continuation based on LINQ Expression
        // Implement Select and SelectMany for the Task type and then apply the composition
        // semantic
        // Suggestion create a Static class or use the partial class TaskEx in Functional.Tasks (Fuctional.cs Library)
        Task<Bitmap> DetectFaces_Task_Composition(string fileName)
        {
            Func<System.Drawing.Rectangle[], Image<Bgr, byte>, Bitmap> drawBoundries = (faces, image) =>
                {
                    faces.ForAll(face => image.Draw(face, new
                        Bgr(System.Drawing.Color.BurlyWood), 3));
                    return image.ToBitmap();
                };


            // TODO code here
            return null;
        }

        void StartFaceDetection_Pipeline(string imagesFolder)
        {
            // The refactor Detect-Face code using the parallel Pipeline
            var files = Directory.GetFiles(ImagesFolder);

            Func<string, Image<Bgr, byte>> imageFn =
                (fileName) => new Image<Bgr, byte>(fileName);
            Func<Image<Bgr, byte>, Tuple<Image<Bgr, byte>, Image<Gray, byte>>> grayFn =
                image => Tuple.Create(image, image.Convert<Gray, byte>());
            Func<Tuple<Image<Bgr, byte>, Image<Gray, byte>>,
                 Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                 CascadeClassifierThreadLocal.Value.DetectMultiScale(
                     frames.Item2, 1.1, 3, System.Drawing.Size.Empty));
            Func<Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>, Bitmap> drawFn =
                faces =>
                {
                    foreach (var face in faces.Item2)
                        faces.Item1.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return faces.Item1.ToBitmap();
                };

            // TODO : 2.8
            // Replace Pipeline implementation with "CsPipeline" (look for TODO : 2.4)
            // suggestion look into the next mehoth that uses the F# Pipeline
            IPipeline<string, Bitmap> imagePipe = null;
                //DataParallelism.Pipelines.CsPipeline




            CancellationTokenSource cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token);

            // TODO uncomment these code after Pipeline implementatation
            //foreach (string fileName in files)
            //    imagePipe.Enqueue(fileName,
            //        ((tup) =>
            //        {
            //            Application.Current.Dispatcher.Invoke(
            //                () =>
            //                Images.Add(tup.Item2.ToBitmapImage()));
            //            return (Unit)Activator.CreateInstance(typeof(Unit), true);
            //        }));
        }

        void StartFaceDetection_Pipeline_FSharpFunc(string imagesFolder)
        {
            var files = Directory.GetFiles(ImagesFolder);

            Func<string, Image<Bgr, byte>> imageFn =
                (fileName) => new Image<Bgr, byte>(fileName);
            Func<Image<Bgr, byte>, Tuple<Image<Bgr, byte>, Image<Gray, byte>>> grayFn =
                image => Tuple.Create(image, image.Convert<Gray, byte>());
            Func<Tuple<Image<Bgr, byte>, Image<Gray, byte>>,
                 Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                 CascadeClassifierThreadLocal.Value.DetectMultiScale(
                     frames.Item2, 1.1, 3, System.Drawing.Size.Empty));
            Func<Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>, Bitmap> drawFn =
                faces =>
                {
                    foreach (var face in faces.Item2)
                        faces.Item1.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return faces.Item1.ToBitmap();
                };

            Pipeline.IPipeline<string, Bitmap> imagePipe = null;

            CancellationTokenSource cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token);

            foreach (string fileName in files)
                imagePipe.Enqueue(fileName,
                    FSharpFuncUtils.Create<Tuple<string, Bitmap>, Unit>
                    ((tup) =>
                    {
                        Application.Current.Dispatcher.Invoke(
                            () => Images.Add(tup.Item2.ToBitmapImage()));
                        return (Unit)Activator.CreateInstance(typeof(Unit), true);
                    }));
        }
    }

    internal static class Helpers
    {
        public static BitmapImage BitmapImageFromStream(this Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Position = 0;
                return BitmapImageFromStream(ms);
            }
        }

        public static T[] ForAll<T>(this T[] array, Action<T> action)
        {
            foreach (var item in array)
                action(item);
            return array;
        }
    }
}
