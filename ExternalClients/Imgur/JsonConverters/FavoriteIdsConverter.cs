using Forum.ExternalClients.Imgur.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Forum.ExternalClients.Imgur.JsonConverters {
	public class FavoriteIdsConverter : JsonConverter {
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.None) {
				return null;
			}

			var readerData = JObject.Load(reader).ToString();
			var responseObject = JsonConvert.DeserializeObject<Response<object[]>>(readerData);
			var jArrayData = JsonConvert.SerializeObject(responseObject.Data);

			var jArray = JArray.Parse(jArrayData);

			var result = new List<string>();

			foreach (var item in jArray) {
				if (item.Value<bool>("is_album")) {
					var album = item.ToObject<GalleryAlbum>();

					if (!(album.Images is null)) {
						foreach (var image in album.Images) {
							if (image.Animated) {
								result.Add(image.Id);
							}
						}
					}
				}
				else {
					var image = item.ToObject<Image>();

					if (image.Animated) {
						result.Add(image.Id);
					}
				}
			}

			return result;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
		public override bool CanConvert(Type objectType) => true;
	}
}
