using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class Feature
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string FeatureKey { get; set; }

        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; }

        public int? ParentFeatureId { get; set; }

        [ForeignKey(nameof(ParentFeatureId))]
        public Feature? ParentFeature { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Feature>? ChildFeatures { get; set; }
        public ICollection<PlanFeature>? PlanFeatures { get; set; }
    }
}
