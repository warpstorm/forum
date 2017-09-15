using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Forum3.Models.InputModels;
using Forum3.Models.ServiceModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage.Blob;

using DataModels = Forum3.Models.DataModels;
using InputModels = Forum3.Models.InputModels;
using ServiceModels = Forum3.Models.ServiceModels;
using ViewModels = Forum3.Models.ViewModels.Smileys;

namespace Forum3.Services {
	public class SmileyService {
		DataModels.ApplicationDbContext DbContext { get; }
		CloudBlobClient CloudBlobClient { get; }

		public SmileyService(
			DataModels.ApplicationDbContext dbContext,
			CloudBlobClient cloudBlobClient
		) {
			DbContext = dbContext;
			CloudBlobClient = cloudBlobClient;
		}

		public async Task<ViewModels.IndexPage> IndexPage() {
			var smileysQuery = from smiley in DbContext.Smileys
							   orderby smiley.SortOrder
							   select smiley;

			var smileys = await smileysQuery.ToListAsync();

			var viewModel = new ViewModels.IndexPage();

			foreach (var smiley in smileys) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				viewModel.Smileys.Add(new ViewModels.IndexSmiley {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return viewModel;
		}

		public ViewModels.CreatePage CreatePage(InputModels.CreateSmileyInput input = null) {
			var viewModel = new ViewModels.CreatePage();

			if (input != null)
				viewModel.Code = input.Code;

			return viewModel;
		}

		public async Task<ServiceModels.ServiceResponse> Create(InputModels.CreateSmileyInput input) {
			var serviceResponse = new ServiceResponse();

			var allowedExtensions = new[] { ".gif" };
			var extension = Path.GetExtension(input.File.FileName);

			if (Regex.IsMatch(input.File.FileName, @"[^a-zA-Z 0-9_\-\.]"))
				serviceResponse.ModelErrors.Add("File", "Your filename contains invalid characters.");

			if (!allowedExtensions.Contains(extension))
				serviceResponse.ModelErrors.Add("File", "Your file must be a gif.");

			if (DbContext.Smileys.Any(s => s.Code == input.Code))
				serviceResponse.ModelErrors.Add(nameof(input.Code), "Another smiley exists with that code.");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			var smileyRecord = new DataModels.Smiley {
				Code = input.Code,
				FileName = input.File.FileName
			};

			await DbContext.Smileys.AddAsync(smileyRecord);

			var container = CloudBlobClient.GetContainerReference("smileys");

			if (await container.CreateIfNotExistsAsync())
				await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

			var blobReference = container.GetBlockBlobReference(input.File.FileName);

			// Multiple smileys can point to the same image.
			if (!await blobReference.ExistsAsync()) {
				blobReference.Properties.ContentType = "image/gif";

				using (var fileStream = input.File.OpenReadStream()) {
					fileStream.Position = 0;
					await blobReference.UploadFromStreamAsync(fileStream);
				}
			}

			smileyRecord.Path = blobReference.Uri.AbsoluteUri;

			await DbContext.SaveChangesAsync();

			serviceResponse.Message = $"Smiley '{smileyRecord.FileName}' was added with code '{smileyRecord.Code}'.";
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Edit(EditSmileysInput input) {
			var serviceResponse = new ServiceResponse();

			foreach (var smileyInput in input.Smileys) {
				var smileyRecord = await DbContext.Smileys.FindAsync(smileyInput.Id);

				if (smileyRecord == null) {
					serviceResponse.ModelErrors.Add(null, $@"No smiley was found with the id '{smileyInput.Id}'");
					break;
				}

				if (smileyRecord.Code != smileyInput.Code) {
					smileyRecord.Code = smileyInput.Code;
					DbContext.Entry(smileyRecord).State = EntityState.Modified;
				}
			}

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			await DbContext.SaveChangesAsync();

			serviceResponse.Message = $"The smiley was updated.";
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Delete(int id) {
			var serviceResponse = new ServiceResponse();

			var smileyRecord = await DbContext.Smileys.FindAsync(id);

			if (smileyRecord == null)
				serviceResponse.ModelErrors.Add(null, $@"No smiley was found with the id '{id}'");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			var otherSmileys = DbContext.Smileys.Where(s => s.FileName == smileyRecord.FileName).ToList();

			DbContext.Smileys.Remove(smileyRecord);

			var container = CloudBlobClient.GetContainerReference("smileys");

			if (!otherSmileys.Any() && await container.ExistsAsync()) {
				var blobReference = container.GetBlockBlobReference(smileyRecord.Path);
				await blobReference.DeleteIfExistsAsync();
			}

			await DbContext.SaveChangesAsync();
			
			serviceResponse.Message = $"The smiley was deleted.";
			return serviceResponse;
		}
	}
}