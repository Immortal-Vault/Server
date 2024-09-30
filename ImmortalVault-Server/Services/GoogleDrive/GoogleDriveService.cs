using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using ImmortalVault_Server.Services.AES;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ImmortalVault_Server.Services.GoogleDrive;

public interface IGoogleDriveService
{
    Task UploadOrReplacePasswordFile(string accessToken, string content);
    Task UploadPasswordFile(string accessToken, string content);
    Task DeletePasswordFile(string accessToken, string content);
    Task<(string Id, string Content)?> GetPasswordFile(string accessToken);
    Task<FileList> GetAllFiles(string accessToken);
    DriveService GetGoogleDriveService(string accessToken);
    Task<bool> DoesPasswordFileExists(string accessToken);
}

public class GoogleDriveService : IGoogleDriveService
{
    private readonly string _aesSecretKey;
    private readonly string _aesIv;
    private const string PasswordFileName = "immortal-vault.pass";

    public GoogleDriveService(IConfiguration configuration)
    {
        this._aesSecretKey = configuration["AES:SECRET_KEY"]!;
        this._aesIv = configuration["AES:IV"]!;
    }
    
    public async Task UploadOrReplacePasswordFile(string accessToken, string content)
    {
        if (await this.DoesPasswordFileExists(accessToken))
        {
            await this.DeletePasswordFile(accessToken, content);
        }

        await this.UploadPasswordFile(accessToken, content);
    }

    public async Task UploadPasswordFile(string accessToken, string content)
    {
        var service = this.GetGoogleDriveService(accessToken);

        var fileMetadata = new DriveFile
        {
            Name = PasswordFileName,
            Parents = new List<string> { "appDataFolder" }
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var uploadRequest = service.Files.Create(fileMetadata, stream, "plain/text");
        uploadRequest.Fields = "id";
        await uploadRequest.UploadAsync();
    }
    
    public async Task DeletePasswordFile(string accessToken, string content)
    {
        var service = this.GetGoogleDriveService(accessToken);
        var passwordFileInfo = await this.GetPasswordFile(accessToken);
        if (passwordFileInfo is not {} info)
        {
            return;
        }

        var deleteFileRequest = service.Files.Delete(info.Id);
        await deleteFileRequest.ExecuteAsync();
    }

    public async Task<(string Id, string Content)?> GetPasswordFile(string accessToken)
    {
        var service = this.GetGoogleDriveService(accessToken);
        var fileList = await this.GetAllFiles(accessToken);

        var fileId = fileList.Files.FirstOrDefault(f => f.Name == PasswordFileName)?.Id;
        if (fileId == null)
        {
            return null;
        }

        var request = service.Files.Get(fileId);
        var stream = new MemoryStream();

        await request.DownloadAsync(stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return (fileId, await reader.ReadToEndAsync());
    }

    public async Task<FileList> GetAllFiles(string accessToken)
    {
        var service = this.GetGoogleDriveService(accessToken);

        var listRequest = service.Files.List();
        listRequest.Spaces = "appDataFolder";
        return await listRequest.ExecuteAsync();
    }

    public DriveService GetGoogleDriveService(string accessToken)
    {
        var decryptedAccessToken = AesEncryption.Decrypt(accessToken, this._aesSecretKey, this._aesIv);
        var credential = GoogleCredential.FromAccessToken(decryptedAccessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        });
    }
    
    public async Task<bool> DoesPasswordFileExists(string accessToken)
    {
        var fileList = await this.GetAllFiles(accessToken);

        var file = fileList.Files.FirstOrDefault(f => f.Name == PasswordFileName);
        return file != null;
    }
}