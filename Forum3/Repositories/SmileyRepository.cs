using Forum3.Contexts;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.Smileys;

	public class SmileyRepository : Repository<DataModels.Smiley> {
		ApplicationDbContext DbContext { get; }
		CloudBlobClient CloudBlobClient { get; }

		public SmileyRepository(
			ApplicationDbContext dbContext,
			CloudBlobClient cloudBlobClient,
			ILogger<SmileyRepository> log
		) : base(log) {
			DbContext = dbContext;
			CloudBlobClient = cloudBlobClient;
		}

		public List<List<ViewModels.IndexItem>> GetSelectorList() {
			var results = new List<List<ViewModels.IndexItem>>();

			var currentColumn = -1;

			List<ViewModels.IndexItem> currentColumnList = null;

			foreach (var smiley in Records) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				if (currentColumn != sortColumn) {
					currentColumn = sortColumn;
					currentColumnList = new List<ViewModels.IndexItem>();
					results.Add(currentColumnList);
				}

				currentColumnList.Add(new ViewModels.IndexItem {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Thought = smiley.Thought,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return results;
		}

		public async Task<ServiceModels.ServiceResponse> Create(InputModels.CreateSmileyInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var allowedExtensions = new[] { "gif", "png" };
			var extension = Path.GetExtension(input.File.FileName).ToLower().Substring(1);

			if (Regex.IsMatch(input.File.FileName, @"[^a-zA-Z 0-9_\-\.]"))
				serviceResponse.Error("File", "Your filename contains invalid characters.");

			if (!allowedExtensions.Contains(extension))
				serviceResponse.Error("File", $"Your file must be: {string.Join(", ", allowedExtensions)}.");

			if (DbContext.Smileys.Any(s => s.Code == input.Code))
				serviceResponse.Error(nameof(input.Code), "Another smiley exists with that code.");

			if (!serviceResponse.Success)
				return serviceResponse;

			var smileyRecord = new DataModels.Smiley {
				Code = input.Code,
				Thought = input.Thought,
				FileName = input.File.FileName
			};

			DbContext.Smileys.Add(smileyRecord);

			var container = CloudBlobClient.GetContainerReference("smileys");

			if (await container.CreateIfNotExistsAsync())
				await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

			var blobReference = container.GetBlockBlobReference(input.File.FileName);

			// Multiple smileys can point to the same image.
			if (!await blobReference.ExistsAsync()) {
				blobReference.Properties.ContentType = $"{input.File.ContentType}";

				using (var fileStream = input.File.OpenReadStream()) {
					fileStream.Position = 0;
					await blobReference.UploadFromStreamAsync(fileStream);
				}
			}

			smileyRecord.Path = blobReference.Uri.AbsoluteUri;

			DbContext.SaveChanges();

			serviceResponse.Message = $"Smiley '{smileyRecord.FileName}' was added with code '{smileyRecord.Code}'.";
			return serviceResponse;
		}

		public ServiceModels.ServiceResponse Update(InputModels.EditSmileysInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var smileySortOrder = new Dictionary<int, int>();

			foreach (var smileyInput in input.Smileys) {
				var smileyRecord = DbContext.Smileys.Find(smileyInput.Id);

				if (smileyRecord is null) {
					serviceResponse.Error($@"No smiley was found with the id '{smileyInput.Id}'");
					break;
				}

				smileySortOrder.Add(smileyRecord.Id, smileyRecord.SortOrder);
			}

			foreach (var smileyInput in input.Smileys) {
				var newSortOrder = (smileyInput.Column * 1000) + smileyInput.Row;

				if (smileySortOrder[smileyInput.Id] != newSortOrder) {
					foreach (var kvp in smileySortOrder.Where(kvp => smileyInput.Column == kvp.Value / 1000 && kvp.Value >= newSortOrder).ToList())
						smileySortOrder[kvp.Key]++;

					smileySortOrder[smileyInput.Id] = newSortOrder;
				}
			}

			foreach (var smileyInput in input.Smileys) {
				var smileyRecord = DbContext.Smileys.Find(smileyInput.Id);

				if (smileyRecord.Code != smileyInput.Code) {
					smileyRecord.Code = smileyInput.Code;
					DbContext.Update(smileyRecord);
				}

				if (smileyRecord.Thought != smileyInput.Thought) {
					smileyRecord.Thought = smileyInput.Thought;
					DbContext.Update(smileyRecord);
				}

				if (smileyRecord.SortOrder != smileySortOrder[smileyRecord.Id]) {
					smileyRecord.SortOrder = smileySortOrder[smileyRecord.Id];
					DbContext.Update(smileyRecord);
				}
			}

			if (!serviceResponse.Success)
				return serviceResponse;

			DbContext.SaveChanges();

			serviceResponse.Message = $"The smiley was updated.";
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Delete(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var smileyRecord = await DbContext.Smileys.FindAsync(id);

			if (smileyRecord is null)
				serviceResponse.Error($@"No smiley was found with the id '{id}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var otherSmileys = DbContext.Smileys.Where(s => s.FileName == smileyRecord.FileName).ToList();

			DbContext.Smileys.Remove(smileyRecord);

			var thoughts = DbContext.MessageThoughts.Where(t => t.SmileyId == id).ToList();

			if (thoughts.Any())
				DbContext.MessageThoughts.RemoveRange(thoughts);

			var container = CloudBlobClient.GetContainerReference("smileys");

			if (!otherSmileys.Any() && await container.ExistsAsync()) {
				var blobReference = container.GetBlockBlobReference(smileyRecord.Path);
				await blobReference.DeleteIfExistsAsync();
			}

			DbContext.SaveChanges();

			serviceResponse.Message = $"The smiley was deleted.";
			return serviceResponse;
		}

		protected override List<DataModels.Smiley> GetRecords() => DbContext.Smileys.Where(s => s.Code != null).OrderBy(s => s.SortOrder).ToList();
	}
}