using Forum.Services.Contexts;
using Forum.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using ControllerModels = Models.ControllerModels;
	using DataModels = Models.DataModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class QuoteRepository : IRepository<DataModels.Quote> {
		public async Task<List<DataModels.Quote>> Records() => _Records ?? (_Records = await DbContext.Quotes.ToListAsync());
		List<DataModels.Quote> _Records;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		MessageRepository MessageRepository { get; }

		public QuoteRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			MessageRepository messageRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			MessageRepository = messageRepository;
		}

		public async Task<ViewModels.Quotes.EditQuotes> Index() {
			var returnObject = new ViewModels.Quotes.EditQuotes {
				Quotes = new List<ViewModels.Quotes.EditQuote>()
			};

			foreach (var record in await Records()) {
				var originalMessage = DbContext.Messages.FirstOrDefault(r => r.Id == record.MessageId);
				var postedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == record.PostedById);
				var submittedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == record.SubmittedById);

				returnObject.Quotes.Add(new ViewModels.Quotes.EditQuote {
					Id = record.Id,
					MessageId = record.MessageId,
					OriginalBody = record.OriginalBody,
					DisplayBody = record.DisplayBody,
					PostedBy = postedBy?.DecoratedName ?? "Missing User",
					PostedTime = originalMessage.TimePosted,
					SubmittedBy = submittedBy?.DecoratedName ?? "Missing User",
					SubmittedTime = record.SubmittedTime,
					Approved = record.Approved
				});
			}

			return returnObject;
		}

		public async Task<ServiceModels.ServiceResponse> Create(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var message = DbContext.Messages.FirstOrDefault(r => r.Id == messageId);

			if (message is null || message.Deleted) {
				serviceResponse.Error($@"No record was found with the id '{messageId}'.");
			}

			if ((await Records()).Any(r => r.MessageId == messageId)) {
				serviceResponse.Error($@"A message with the id '{messageId}' has already been submitted.");
			}

			if (serviceResponse.Success) {
				DbContext.Quotes.Add(new DataModels.Quote {
					MessageId = message.Id,
					PostedById = message.PostedById,
					TimePosted = message.TimePosted,
					SubmittedById = UserContext.ApplicationUser.Id,
					SubmittedTime = DateTime.Now,
					Approved = false,
					OriginalBody = message.OriginalBody,
					DisplayBody = message.DisplayBody
				});

				DbContext.SaveChanges();

				serviceResponse.Message = $"Quote added.";
			}

			return serviceResponse;
		}

		public async Task<ControllerModels.Quotes.EditResult> Edit(ControllerModels.Quotes.QuotesInput input) {
			var result = new ControllerModels.Quotes.EditResult();

			foreach (var quoteInput in input.Quotes) {
				var records = await Records();
				var record = records.FirstOrDefault(r => r.Id == quoteInput.Id);

				if (record is null) {
					result.Errors.Add(nameof(quoteInput.Id), $@"No record was found with the id '{quoteInput.Id}'.");
					break;
				}

				if (quoteInput.Approved != record.Approved) {
					record.Approved = quoteInput.Approved;
					DbContext.Update(record);
				}

				if (quoteInput.OriginalBody != record.OriginalBody) {
					record.OriginalBody = quoteInput.OriginalBody;

					var processedMessage = await MessageRepository.ProcessMessageInput(quoteInput.OriginalBody);

					foreach (var error in processedMessage.Errors) {
						result.Errors.Add(error.Key, error.Value);
					}

					if (!result.Errors.Any()) {
						record.DisplayBody = processedMessage.DisplayBody;
						DbContext.Update(record);
					}
				}

				if (!result.Errors.Any()) {
					await DbContext.SaveChangesAsync();
				}
			}

			return result;
		}

		public async Task<ServiceModels.ServiceResponse> Delete(int quoteId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = (await Records()).FirstOrDefault(r => r.Id == quoteId);

			if (record is null) {
				serviceResponse.Error($@"No record was found with the id '{quoteId}'.");
			}

			if (serviceResponse.Success) {
				DbContext.Quotes.Remove(record);
				await DbContext.SaveChangesAsync();

				serviceResponse.Message = $"Quote deleted.";
			}

			return serviceResponse;
		}
	}
}
