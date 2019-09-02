using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Forum.Core.Annotations {
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class MustBeAfterAttribute : ValidationAttribute {
		string Target { get; }

		public MustBeAfterAttribute(string target) => Target = target;

		protected override ValidationResult IsValid(object value, ValidationContext context) {
			var thisValue = value as DateTime?;

			var property = context.ObjectType.GetProperty(Target);

			if (property is null) {
				throw new ArgumentException($"A property with the name {Target} was not found.");
			}

			var thatValue = property.GetValue(context.ObjectInstance) as DateTime?;

			if (thisValue >= thatValue || (thisValue is null && thatValue is null)) {
				return ValidationResult.Success;
			}

			ErrorMessage = string.Format(CultureInfo.CurrentCulture, ErrorMessageString, new [] { Target });

			return new ValidationResult(ErrorMessage);
		}
	}
}
