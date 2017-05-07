using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Forum3.Annotations {
	public class MaxFileSizeAttribute : ValidationAttribute {
		int MaxFileSize { get; }

		public MaxFileSizeAttribute(int maxFileSize) {
			MaxFileSize = maxFileSize * 1024;
		}

		public override bool IsValid(object value) {
			var file = value as IFormFile;

			if (file == null)
				return false;

			return file.Length <= MaxFileSize;
		}

		public override string FormatErrorMessage(string name) {
			return base.FormatErrorMessage((MaxFileSize / 1024).ToString());
		}
	}
}