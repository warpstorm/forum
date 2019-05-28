using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.Annotations {
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class MustBeAfterAttribute : ValidationAttribute {
		string Target { get; }

		public MustBeAfterAttribute(string target) => Target = target;

		protected override ValidationResult IsValid(object value, ValidationContext context) {
			ErrorMessage = ErrorMessageString;

			var thisValue = (DateTime)value;

			var property = context.ObjectType.GetProperty(Target);

			if (property is null) {
				throw new ArgumentException("Property with this name not found");
			}

			var thatValue = (DateTime)property.GetValue(context.ObjectInstance);

			if (thisValue < thatValue) {
				return new ValidationResult(ErrorMessage);
			}

			return ValidationResult.Success;
		}
	}
}
