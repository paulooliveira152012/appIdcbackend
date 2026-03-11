using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using Microsoft.AspNetCore.Mvc;

namespace BibliaStudy.Api.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public FilesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public class UploadImageRequest
    {
        public string ProfileImage { get; set; } = string.Empty;
    }

    [HttpPost("uploadImage")]
    public async Task<IActionResult> UploadImage([FromBody] UploadImageRequest request)
    {
        Console.WriteLine("uploadImage");
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProfileImage))
            {
                return BadRequest(new { message = "Imagem não enviada." });
            }

            // var bucketName = _configuration["AWS_BUCKET_NAME"];
            // var regionName = _configuration["AWS_REGION"];
            // var accessKey = _configuration["AWS_ACCESS_KEY_ID"];
            // var secretKey = _configuration["AWS_SECRET_ACCESS_KEY"];


            var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");
            var regionName = Environment.GetEnvironmentVariable("AWS_REGION");
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");


            if (string.IsNullOrWhiteSpace(bucketName) ||
                string.IsNullOrWhiteSpace(regionName) ||
                string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey))
            {
                return StatusCode(500, new { message = "Configurações AWS ausentes." });
            }

            if (!request.ProfileImage.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Formato inválido. Envie a imagem em base64 data URL." });
            }

            var commaIndex = request.ProfileImage.IndexOf(',');
            if (commaIndex < 0)
            {
                return BadRequest(new { message = "Base64 inválido." });
            }

            var metadataPart = request.ProfileImage.Substring(0, commaIndex);
            var base64Part = request.ProfileImage.Substring(commaIndex + 1);

            var extension = GetImageExtension(metadataPart);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return BadRequest(new { message = "Tipo de imagem não suportado." });
            }

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Part);
            }
            catch
            {
                return BadRequest(new { message = "Conteúdo base64 inválido." });
            }

            var fileName = $"profiles/{Guid.NewGuid()}.{extension}";
            var region = RegionEndpoint.GetBySystemName(regionName);

            using var s3Client = new AmazonS3Client(accessKey, secretKey, region);
            using var stream = new MemoryStream(imageBytes);

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = GetContentType(extension),
                // CannedACL = S3CannedACL.PublicRead
            };

            await s3Client.PutObjectAsync(putRequest);

            var fileUrl = $"https://{bucketName}.s3.{regionName}.amazonaws.com/{fileName}";

            return Ok(new
            {
                message = "Imagem enviada com sucesso.",
                link = fileUrl
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            return StatusCode(500, new
            {
                message = "Erro ao enviar imagem para o S3.",
                error = s3Ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Erro interno ao processar upload da imagem.",
                error = ex.Message
            });
        }
    }

    private static string GetImageExtension(string metadataPart)
    {
        if (metadataPart.Contains("image/png", StringComparison.OrdinalIgnoreCase)) return "png";
        if (metadataPart.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase)) return "jpg";
        if (metadataPart.Contains("image/jpg", StringComparison.OrdinalIgnoreCase)) return "jpg";
        if (metadataPart.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) return "webp";

        return string.Empty;
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            "png" => "image/png",
            "jpg" => "image/jpeg",
            "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}