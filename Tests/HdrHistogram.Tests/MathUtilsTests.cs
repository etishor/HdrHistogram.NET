using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{
    public class MathUtilsTests
    {
        [Fact]
        public void MathUtils_NumberOfLadingZero()
        {
            MathUtils.NumberOfLeadingZeros(0x0L).Should().Be(64);
            MathUtils.NumberOfLeadingZeros(0x1).Should().Be(63);
            MathUtils.NumberOfLeadingZeros(0x2).Should().Be(62);
            MathUtils.NumberOfLeadingZeros(0x3).Should().Be(62);
            MathUtils.NumberOfLeadingZeros(0x4).Should().Be(61);
            MathUtils.NumberOfLeadingZeros(0x5).Should().Be(61);
            MathUtils.NumberOfLeadingZeros(0x6).Should().Be(61);
            MathUtils.NumberOfLeadingZeros(0x7).Should().Be(61);
            MathUtils.NumberOfLeadingZeros(0x8).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0x9).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xA).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xB).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xC).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xD).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xE).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0xF).Should().Be(60);
            MathUtils.NumberOfLeadingZeros(0x10).Should().Be(59);
            MathUtils.NumberOfLeadingZeros(0x80).Should().Be(56);
            MathUtils.NumberOfLeadingZeros(0xF0).Should().Be(56);
            MathUtils.NumberOfLeadingZeros(0x100).Should().Be(55);
            MathUtils.NumberOfLeadingZeros(0x800).Should().Be(52);
            MathUtils.NumberOfLeadingZeros(0xF00).Should().Be(52);
            MathUtils.NumberOfLeadingZeros(0x1000).Should().Be(51);
            MathUtils.NumberOfLeadingZeros(0x8000).Should().Be(48);
            MathUtils.NumberOfLeadingZeros(0xF000).Should().Be(48);
            MathUtils.NumberOfLeadingZeros(0x10000).Should().Be(47);
            MathUtils.NumberOfLeadingZeros(0x80000).Should().Be(44);
            MathUtils.NumberOfLeadingZeros(0xF0000).Should().Be(44);
            MathUtils.NumberOfLeadingZeros(0x100000).Should().Be(43);
            MathUtils.NumberOfLeadingZeros(0x800000).Should().Be(40);
            MathUtils.NumberOfLeadingZeros(0xF00000).Should().Be(40);
            MathUtils.NumberOfLeadingZeros(0x1000000).Should().Be(39);
            MathUtils.NumberOfLeadingZeros(0x8000000).Should().Be(36);
            MathUtils.NumberOfLeadingZeros(0xF000000).Should().Be(36);
            MathUtils.NumberOfLeadingZeros(0x10000000).Should().Be(35);
            //0x80000000 = c# -2147483648
            MathUtils.NumberOfLeadingZeros(-2147483648).Should().Be(0);
            // java 0xF0000000 = c# -268435456
            MathUtils.NumberOfLeadingZeros(-268435456).Should().Be(0);

            MathUtils.NumberOfLeadingZeros(long.MaxValue).Should().Be(1);
            MathUtils.NumberOfLeadingZeros(long.MinValue).Should().Be(0);
        }
    }
}
