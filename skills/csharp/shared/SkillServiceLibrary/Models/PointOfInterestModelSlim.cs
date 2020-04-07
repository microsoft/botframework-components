using System;
using System.Collections.Generic;
using System.Text;

namespace SkillServiceLibrary.Models
{
    public class PointOfInterestModelSlim
    {
        public PointOfInterestModelSlim (PointOfInterestModel largerModel) {
            PointOfInterestImage = largerModel.PointOfInterestImageUrl;
            Name = largerModel.Name;
            Address = largerModel.Address;
            Price = largerModel.Price;
            Rating = largerModel.Rating;
            Phone = largerModel.Phone;
            Category = largerModel.Category;
            Website = largerModel.Website;
            Hours = largerModel.Hours;
        }

        /// <summary>
        /// Gets or sets the thumbnail image url of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The image URL of this point of interest.
        /// </value>
        public string PointOfInterestImage { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The name of this point of interest.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the formatted address of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The formatted address of this point of interest.
        /// </value>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the price level of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The price of this point of interest.
        /// </value>
        public int Price { get; set; }

        /// <summary>
        /// Gets or sets the rating of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The rating of this point of interest.
        /// </value>
        public string Rating { get; set; }

        /// <summary>
        /// Gets or sets phone.
        /// </summary>
        /// <value>
        /// Phone.
        /// </value>
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the top category of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The top category of this point of interest.
        /// </value>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets Website address.
        /// </summary>
        /// <value>
        /// Website address.
        /// </value>
        public string Website { get; set; }

        /// <summary>
        /// Gets or sets the hours of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The open hours of this point of interest.
        /// </value>
        public string Hours { get; set; }
    }
}
