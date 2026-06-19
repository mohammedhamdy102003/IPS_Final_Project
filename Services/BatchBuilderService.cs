using IPS_PROJECT.Models;

namespace IPS_PROJECT.Services
{
    public class BatchBuilderService
    {
        public EVENTS BuildBatch(List<EVENTS> batch)
        {
            if (batch.Count != 20)
            {
                throw new InvalidOperationException(
                    "Batch must contain exactly 20 events."
                );
            }

            var latest = batch
                .OrderByDescending(e => e.Timestamp)
                .First();

            var benignCount = batch.Count(e =>
                e.AttackType.Equals("Benign",
                StringComparison.OrdinalIgnoreCase));

            var attackCount = batch.Count - benignCount;

            string batchPrediction;
            double confidence;
            string status;
            string prediction;

            if (benignCount >= attackCount)
            {
                batchPrediction = "Benign";

                confidence = batch
                    .Where(e => e.AttackType.Equals("Benign",
                    StringComparison.OrdinalIgnoreCase))
                    .DefaultIfEmpty(latest)
                    .Average(e => e.Confidence);

                status = "Allowed";
                prediction = "Not Anomaly";
            }
            else
            {
                var topAttack = batch
                    .Where(e => !e.AttackType.Equals("Benign",
                    StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.AttackType)
                    .OrderByDescending(g => g.Count())
                    .ThenByDescending(g => g.Average(e => e.Confidence))
                    .First();

                batchPrediction = topAttack.Key;
                confidence = topAttack.Average(e => e.Confidence);

                status = "Blocked";
                prediction = "Anomaly";
            }

            return new EVENTS
            {
                Id = latest.Id,
                Timestamp = latest.Timestamp,
                SourceIp = latest.SourceIp,
                DestinationIp = latest.DestinationIp,
                Prediction = prediction,
                AttackType = batchPrediction,
                Confidence = Math.Round(confidence, 1),
                Status = status
            };
        }
    }
}