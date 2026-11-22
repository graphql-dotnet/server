using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
using ImageMagick;

namespace Samples.Upload;

public class Mutation
{
    public static async Task<string> Rotate([MediaType("image/*")] IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new ExecutionError("File is null or empty.");
        }

        try
        {
            // Read the file into an Image
            using var sourceStream = file.OpenReadStream();
            using var image = new MagickImage();
            await image.ReadAsync(sourceStream, cancellationToken);

            // Rotate the image 90 degrees
            image.Rotate(90);

            // Convert the image to a byte array
            await using var memoryStream = new MemoryStream();
            await image.WriteAsync(memoryStream, MagickFormat.Jpeg, cancellationToken);
            byte[] imageBytes = memoryStream.ToArray();

            // Convert byte array to a base-64 string
            string base64String = Convert.ToBase64String(imageBytes);

            return base64String;
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., file is not an image, or unsupported image format)
            throw new ExecutionError("Error processing image: " + ex.Message, ex);
        }
    }
}
