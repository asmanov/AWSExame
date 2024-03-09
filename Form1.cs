using Amazon.Rekognition.Model;
using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.Net;
using System.Text;

namespace AWSExame
{
    public partial class Form1 : Form
    {
        static BasicAWSCredentials credentials = new BasicAWSCredentials("******", "********");
        string bucketName = "forimagebucket";
        string keyName = "";
        string filePath;
        string photo;
        IAmazonS3 client = new AmazonS3Client(credentials, Amazon.RegionEndpoint.USEast1);
        IAmazonRekognition rekognitionClient = new AmazonRekognitionClient(credentials, Amazon.RegionEndpoint.USEast1);
        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string sourceUrl = textBox2.Text; // URL изображения, которое вы хотите загрузить
            keyName = "image.jpg";
            try
            {
                using (var clientweb = new WebClient())
                {
                    // Загружаем изображение по URL
                    byte[] imageData = clientweb.DownloadData(sourceUrl);

                    // Инициализируем клиента Amazon S3
                    
                        // Создаем объект TransferUtility
                     var transferUtility = new TransferUtility(client);

                        // Загружаем данные изображения в S3
                     await transferUtility.UploadAsync(new System.IO.MemoryStream(imageData), bucketName, keyName);
                    

                    MessageBox.Show("Изображение успешно загружено в S3 бакет.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения в S3: {ex.Message}");
            }
            // Create a GetObject request
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = keyName
            };

            // Issue request and remember to dispose of the response
            using (GetObjectResponse response = await client.GetObjectAsync(request))
            {
                // Save object to local file
                await response.WriteResponseStreamToFileAsync("Item.txt", false, new CancellationTokenSource().Token);
            }
            //textBox1.Text = string.Empty;
            pictureBox1.Image = System.Drawing.Image.FromFile("Item.txt");
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            photo = keyName;
            Recognition();
            //try
            //{
                
            //        // Создаем запрос на удаление объекта из бакета
            //     var deleteRequest = new DeleteObjectRequest
            //     {
            //         BucketName = bucketName,
            //         Key = keyName
            //     };

            //        // Удаляем объект из бакета
            //     await client.DeleteObjectAsync(deleteRequest);

            //    MessageBox.Show($"Файл {keyName} успешно удален из бакета {bucketName}");
                
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Ошибка при удалении файла из бакета: {ex.Message}");
            //}
            //if (File.Exists("Item.txt"))
            //{
            //    // Удаляем файл
            //    File.Delete("Item1.txt");
            //    MessageBox.Show($"Файл Item.txt успешно удален.");
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "images files (*.png;*.jpg)|*.png;*.jpg";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = dialog.FileName;
                    photo = Path.GetFileName(filePath);
                    //textBox1.Text = string.Empty;
                    pictureBox1.Image = System.Drawing.Image.FromFile(filePath);
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    Upload();
                    Recognition();

                }
            }
        }

        private void Upload()
        {
            
            try
            {
                keyName = Path.GetFileName(filePath);
                // Upload the file to Amazon S3
                TransferUtility fileTransferUtility = new TransferUtility(client);
                fileTransferUtility.Upload(filePath, bucketName, keyName);
                MessageBox.Show("Upload completed!");
            }
            catch (AmazonS3Exception e)
            {
                MessageBox.Show("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        private async Task Recognition()
        {
            
            var detectFacesRequest = new DetectFacesRequest()
            {
                Image = new Amazon.Rekognition.Model.Image()
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object()
                    {
                        Name = photo,
                        Bucket = bucketName,
                    },
                },

                // Attributes can be "ALL" or "DEFAULT".
                // "DEFAULT": BoundingBox, Confidence, Landmarks, Pose, and Quality.
                // "ALL": See https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Rekognition/TFaceDetail.html
                Attributes = new List<string>() { "ALL" },
            };
            
            try
            {
                DetectFacesResponse detectFacesResponse = await rekognitionClient.DetectFacesAsync(detectFacesRequest);
                bool hasAll = detectFacesRequest.Attributes.Contains("ALL");
                foreach (FaceDetail face in detectFacesResponse.FaceDetails)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in face.Emotions)
                    {
                        sb.Append(item.Type + " ");
                    }
                    textBox1.Text = $"{sb}";
                    if (hasAll)
                    {
                        //textBox1.Text = $"Estimated age is between {face.AgeRange.Low} and {face.AgeRange.High} years old.";

                    }
                    Bitmap bmp = new Bitmap(pictureBox1.Image);
                    using (Graphics g = Graphics.FromImage(bmp))
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        int left = (int)(face.BoundingBox.Left * bmp.Width);
                        int top = (int)(face.BoundingBox.Top * bmp.Height);
                        int width = (int)(face.BoundingBox.Width * bmp.Width);
                        int height = (int)(face.BoundingBox.Height * bmp.Height);

                        Rectangle rect = new Rectangle(left, top, width, height);
                        g.DrawRectangle(pen, rect);
                    }
                    pictureBox1.Image = bmp;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            textBox2.Text = string.Empty;
            photo = string.Empty;
        }
    }
}
