using System;

namespace GntTools.Swl
{
    /// <summary>하수관로 표고 계산 결과</summary>
    public class ElevationResult
    {
        public double BeginInvertLevel { get; set; }  // 시점관저고
        public double EndInvertLevel { get; set; }    // 종점관저고
        public double BeginCrownLevel { get; set; }   // 시점관상고
        public double EndCrownLevel { get; set; }     // 종점관상고
        public double Slope { get; set; }             // 구배(%)
    }

    /// <summary>관저고/관상고/구배 자동 계산</summary>
    public static class ElevationCalculator
    {
        /// <summary>
        /// 표고 계산
        /// </summary>
        /// <param name="beginGroundHeight">시점지반고 (m)</param>
        /// <param name="endGroundHeight">종점지반고 (m)</param>
        /// <param name="beginDepth">시점심도 (m)</param>
        /// <param name="endDepth">종점심도 (m)</param>
        /// <param name="diameterMeter">구경 (m 단위, 예: 0.3)</param>
        /// <param name="pipeLength">관로 연장 (m)</param>
        public static ElevationResult Calculate(
            double beginGroundHeight, double endGroundHeight,
            double beginDepth, double endDepth,
            double diameterMeter, double pipeLength)
        {
            // 관저고 = 지반고 - 심도
            double beginInvert = beginGroundHeight - beginDepth;
            double endInvert = endGroundHeight - endDepth;

            // 관상고 = 관저고 + 구경(m)
            double beginCrown = beginInvert + diameterMeter;
            double endCrown = endInvert + diameterMeter;

            // 구배 = (시점관저고 - 종점관저고) / 연장 × 100
            double slope = 0;
            if (pipeLength > 0)
                slope = Math.Round(
                    (beginInvert - endInvert) / pipeLength * 100, 3);

            return new ElevationResult
            {
                BeginInvertLevel = Math.Round(beginInvert, 3),
                EndInvertLevel = Math.Round(endInvert, 3),
                BeginCrownLevel = Math.Round(beginCrown, 3),
                EndCrownLevel = Math.Round(endCrown, 3),
                Slope = slope,
            };
        }

        /// <summary>구경 문자열(mm) → m 변환</summary>
        public static double DiameterToMeter(string diameterStr)
        {
            if (double.TryParse(diameterStr, out double mm))
                return mm / 1000.0;
            return 0;
        }
    }
}
