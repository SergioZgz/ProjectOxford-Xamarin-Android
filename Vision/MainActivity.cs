namespace Vision
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Android.App;
    using Android.Content.PM;
    using Android.Graphics;
    using Android.OS;
    using Android.Widget;

    using Microsoft.ProjectOxford.Emotion;
    using Microsoft.ProjectOxford.Vision;
    using Microsoft.ProjectOxford.Vision.Contract;

    using Plugin.Media;
    using Plugin.Media.Abstractions;
    
    using Color = Android.Graphics.Color;
    using Debug = System.Diagnostics.Debug;

    [Activity(Label = "Vision", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const string emotionKey = "YOUR KEY";
        private const string visionKey = "YOUR KEY";
        private ImageView imageContainer;
        private TextView textview;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);
            
            this.imageContainer = this.FindViewById<ImageView>(Resource.Id.imageView1);
            this.textview = this.FindViewById<TextView>(Resource.Id.content);

            this.FindViewById<Button>(Resource.Id.InfoButton).Click += this.InfoButtonClick;
            this.FindViewById<Button>(Resource.Id.EmocionButton).Click += this.EmotionButtonClick;
        }

        private async void EmotionButtonClick(object sender, EventArgs e)
        {
            Bitmap resized = await this.TakePhoto();

            using (MemoryStream stream = new MemoryStream())
            {
                resized.Compress(Bitmap.CompressFormat.Jpeg, 90, stream);
                stream.Seek(0, SeekOrigin.Begin);
                
                EmotionServiceClient emotionClient = new EmotionServiceClient(emotionKey);

                var emotionResults = await emotionClient.RecognizeAsync(stream);

                if (emotionResults == null || !emotionResults.Any())
                {
                    throw new Exception("Can't detect face");
                }

                Canvas canvas = new Canvas(resized);
                foreach (var emotionResult in emotionResults)
                {
                    Math.Round(emotionResult.Scores.Happiness * 100, 2);

                    var rect = emotionResult.FaceRectangle;

                    Paint paint = new Paint(PaintFlags.AntiAlias);
                    paint.StrokeWidth = 10;
                    paint.Color = Color.Rgb(200, 00, 200);

                    canvas.DrawLine(rect.Left, rect.Top, rect.Left, rect.Top + rect.Height, paint);
                    canvas.DrawLine(rect.Left, rect.Top + rect.Height, rect.Left + rect.Width, rect.Top + rect.Height, paint);
                    canvas.DrawLine(rect.Left + rect.Width, rect.Top + rect.Height, rect.Left + rect.Width, rect.Top, paint);
                    canvas.DrawLine(rect.Left + rect.Width, rect.Top, rect.Left, rect.Top, paint);
                    
                    this.textview.Text += $"Enfado: {Math.Round(emotionResult.Scores.Anger * 100, 2)}%\n";
                    this.textview.Text += $"Desprecio: {Math.Round(emotionResult.Scores.Contempt * 100, 2)}%\n";
                    this.textview.Text += $"Asco: {Math.Round(emotionResult.Scores.Disgust * 100, 2)}%\n";
                    this.textview.Text += $"Miedo: {Math.Round(emotionResult.Scores.Fear * 100, 2)}%\n";
                    this.textview.Text += $"Neutral: {Math.Round(emotionResult.Scores.Neutral * 100, 2)}%\n";
                    this.textview.Text += $"Tristeza: {Math.Round(emotionResult.Scores.Sadness * 100, 2)}%\n";
                    this.textview.Text += $"Sorpresa: {Math.Round(emotionResult.Scores.Surprise * 100, 2)}%\n";
                }
            }

            this.imageContainer.SetImageBitmap(resized);
        }

        private async void InfoButtonClick(object sender, EventArgs e)
        {
            Bitmap resized = await this.TakePhoto();

            VisionServiceClient visionClient = new VisionServiceClient(visionKey);
            VisualFeature[] features = { VisualFeature.Tags, VisualFeature.Categories, VisualFeature.Description };

            using (MemoryStream stream = new MemoryStream())
            {
                resized.Compress(Bitmap.CompressFormat.Jpeg, 90, stream);
                stream.Seek(0, SeekOrigin.Begin);

                var data = await visionClient.AnalyzeImageAsync(stream, features, null);

                foreach (Caption caption in data.Description.Captions)
                {
                    this.textview.Text += caption.Text + "\n";
                }
            }

            this.imageContainer.SetImageBitmap(resized);
        }

        private async Task<Bitmap> TakePhoto()
        {
            var media = new MediaImplementation();
            var file =
                await
                CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions { Directory = "Sample", Name = "test.jpg", SaveToAlbum = true, DefaultCamera = CameraDevice.Front });
            var path = file.Path;
            Debug.WriteLine(path);
            Bitmap original = BitmapFactory.DecodeFile(file.Path);
            file.Dispose();
            Bitmap resized = Bitmap.CreateScaledBitmap(original, 320, 480, false);

            this.textview.Text = string.Empty;
            return resized;
        }
    }
}