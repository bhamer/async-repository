using System;

namespace AsyncRepository.Models
{
    public class Position
    {
        public int PositionId { get; set; }
        public DateTime PositionDate { get; set; }
        public decimal MarketValue { get; set; }
        public string AccountCode { get; set; }
        public int SecurityId { get; set; }
    }
}
