using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Annotations {
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class MaxFileSizeAttribute : ValidationAttribute {
		int MaxFileSize { get; }

		public MaxFileSizeAttribute(int maxFileSize) {
			MaxFileSize = maxFileSize * 1024;
		}

		public override bool IsValid(object value) {
			var file = value as IFormFile;

			if (file is null) {
				return false;
			}

			return file.Length <= MaxFileSize;
		}

		public override string FormatErrorMessage(string name) => base.FormatErrorMessage((MaxFileSize / 1024).ToString());
	}
}