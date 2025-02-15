using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using ImmortalVault_Server.Services.AES;
using ImmortalVault_Server.Utils;
using Microsoft.EntityFrameworkCore;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using User = ImmortalVault_Server.Models.User;

namespace ImmortalVault_Server.Services.GoogleDrive;

public interface IGoogleDriveService
{
    Task<(bool Result, bool Conflict, string? Hash)> UploadOrReplaceSecretFile(User user, string content, string hash);
    Task<(bool Result, bool Conflict, string? Hash)> UploadSecretFile(User user, string content, string hash);
    Task DeleteSecretFile(User user);
    Task<(string Id, string Content, string Hash)?> GetSecretFile(User user);
    Task<FileList> GetAllFiles(User user);
    DriveService? GetGoogleDriveService(User user);
    Task<bool> DoesSecretFileExists(User user);
    Task<bool> UpdateTokens(User user);
    bool IsTokenExpired(User user);
}

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    private readonly string _aesSecretKey;
    private readonly string _aesIv;
    private const string SecretFileName = "immortal-vault.pass";

    public GoogleDriveService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        this._configuration = configuration;
        this._dbContext = dbContext;

        this._aesSecretKey = configuration["AES:SECRET_KEY"]!;
        this._aesIv = configuration["AES:IV"]!;
    }

    public async Task<(bool Result, bool Conflict, string? Hash)> UploadOrReplaceSecretFile(User user, string content,
        string hash)
    {
        return await this.UploadSecretFile(user, content, hash);
    }

    public async Task<(bool Result, bool Conflict, string? Hash)> UploadSecretFile(User user, string content,
        string hash)
    {
        var service = this.GetGoogleDriveService(user);
        if (service is null)
        {
            return (false, false, null);
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var serverSecretFile = await this.GetSecretFile(user);

        if (serverSecretFile is not null)
        {
            if (!serverSecretFile.Value.Hash.SequenceEqual(hash))
            {
                return (false, true, serverSecretFile.Value.Hash);
            }

            stream.Position = 0;
            var updateRequest = service.Files.Update(new DriveFile(), serverSecretFile.Value.Id, stream, "plain/text");
            updateRequest.Fields = "id";
            await updateRequest.UploadAsync();
        }
        else
        {
            stream.Position = 0;
            var secretFileMetadata = new DriveFile
            {
                Name = SecretFileName,
                Parents = new List<string> { "appDataFolder" }
            };

            var createRequest = service.Files.Create(secretFileMetadata, stream, "plain/text");
            createRequest.Fields = "id";
            await createRequest.UploadAsync();
        }

        var newHash = HashUtils.ComputeSHA256Hash(content);
        return (true, false, newHash);
    }

    public async Task DeleteSecretFile(User user)
    {
        var service = this.GetGoogleDriveService(user);
        if (service is null)
        {
            return;
        }

        var passwordFileInfo = await this.GetSecretFile(user);
        if (passwordFileInfo is not { } info)
        {
            return;
        }

        var deleteFileRequest = service.Files.Delete(info.Id);
        await deleteFileRequest.ExecuteAsync();
    }

    public async Task<(string Id, string Content, string Hash)?> GetSecretFile(User user)
    {
        var service = this.GetGoogleDriveService(user);
        if (service is null)
        {
            return null;
        }

        var fileList = await this.GetAllFiles(user);

        var fileId = fileList.Files.FirstOrDefault(f => f.Name == SecretFileName)?.Id;
        if (fileId == null)
        {
            return null;
        }

        var request = service.Files.Get(fileId);
        var stream = new MemoryStream();

        await request.DownloadAsync(stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync();
        var hash = HashUtils.ComputeSHA256Hash(content);

        return (fileId, content, hash);
    }

    public async Task<FileList> GetAllFiles(User user)
    {
        var service = this.GetGoogleDriveService(user);
        if (service is null)
        {
            return new FileList();
        }

        var listRequest = service.Files.List();
        listRequest.Spaces = "appDataFolder";
        return await listRequest.ExecuteAsync();
    }

    public DriveService? GetGoogleDriveService(User user)
    {
        if (user.UserTokens is not { } tokens)
        {
            return null;
        }

        var decryptedAccessToken = AesEncryption.Decrypt(tokens.AccessToken, this._aesSecretKey, this._aesIv);
        var credential = GoogleCredential.FromAccessToken(decryptedAccessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        });
    }

    public async Task<bool> DoesSecretFileExists(User user)
    {
        var fileList = await this.GetAllFiles(user);

        var file = fileList.Files.FirstOrDefault(f => f.Name == SecretFileName);
        return file != null;
    }

    public async Task<bool> UpdateTokens(User user)
    {
        if (user.UserTokens is not { } tokens)
        {
            return false;
        }

        var oAuth2Client = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = this._configuration["GOOGLE:CLIENT_ID"],
                ClientSecret = this._configuration["GOOGLE:CLIENT_SECRET"],
            }
        });

        var decryptedRefreshToken = AesEncryption.Decrypt(tokens.RefreshToken, this._aesSecretKey, this._aesIv);
        var tokenResponse =
            await oAuth2Client.RefreshTokenAsync(user.Id.ToString(), decryptedRefreshToken, CancellationToken.None);

        if (tokenResponse is null)
        {
            return false;
        }

        var encryptedAccessToken = AesEncryption.Encrypt(tokenResponse.AccessToken, this._aesSecretKey, this._aesIv);
        var encryptedRefreshToken = AesEncryption.Encrypt(tokenResponse.RefreshToken, this._aesSecretKey, this._aesIv);
        var tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds.GetValueOrDefault(3600));

        user.UserTokens.AccessToken = encryptedAccessToken;
        user.UserTokens.RefreshToken = encryptedRefreshToken;
        user.UserTokens.TokenExpiryTime = tokenExpiryTime;

        await this._dbContext.UsersTokens
            .Where(t => t.Id == tokens.Id)
            .ExecuteUpdateAsync(t => t
                .SetProperty(t => t.AccessToken, encryptedAccessToken)
                .SetProperty(t => t.RefreshToken, encryptedRefreshToken)
                .SetProperty(t => t.TokenExpiryTime, tokenExpiryTime)
            );

        return true;
    }

    public bool IsTokenExpired(User user)
    {
        if (user.UserTokens is not { } tokens)
        {
            return true;
        }

        return tokens.TokenExpiryTime <= DateTime.UtcNow;
    }
}