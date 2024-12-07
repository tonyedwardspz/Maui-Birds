using System;
namespace Maui_Birds.Models
{
	public class Bird
	{
		public int Id { get; set; }
		public string? CommonName { get; set; }
		public string? Image { get; set; }
		public string ImageFilename => $"{CommonName.Replace(" ", "_").ToLower()}.jpg";
		public string? ImageCredit { get; set; }
		public string? Song { get; set; }
		public string? SongCredit { get; set; }
		public int? Sightings { get; set; }
	}
}

