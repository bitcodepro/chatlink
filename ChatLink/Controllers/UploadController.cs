using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatLink.Controllers;

[Authorize]
[Route("api/chatlink/[controller]")]
[ApiController]
public class UploadController : ControllerBase
{
    [HttpGet("{sessionId}/{fileName}")]
    public IActionResult GetFile(string sessionId, string fileName)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var filePath = Path.Combine(uploadsFolder, Path.Combine(sessionId ,fileName));

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var encryptedData = System.IO.File.ReadAllBytes(filePath);

        return File(encryptedData, "application/octet-stream");
    }

    [HttpPost("{sessionId}")]
    public async Task<IActionResult> Upload(string sessionId, IFormFile? file)
    {
        if (file == null || file.Length == 0 || string.IsNullOrWhiteSpace(sessionId))
            return BadRequest("No file uploaded.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), $"uploads/{sessionId}");
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, file.FileName);

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            var fileData = stream.ToArray();

            await System.IO.File.WriteAllBytesAsync(filePath, fileData);
        }

        var fileUrl = $"{Request.Scheme}://{Request.Host}/api/chatlink/upload/{sessionId}/{file.FileName}";

        return Ok(new { file.FileName, fileUrl });
    }
}
