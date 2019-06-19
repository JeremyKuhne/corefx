// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class EnumConverterTests
    {
        [Fact]
        public void ConvertDayOfWeek()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            WhenClass when = JsonSerializer.Parse<WhenClass>(@"{""Day"":""Monday""}", options);
            Assert.Equal(DayOfWeek.Monday, when.Day);
            DayOfWeek day = JsonSerializer.Parse<DayOfWeek>(@"""Tuesday""", options);
            Assert.Equal(DayOfWeek.Tuesday, day);
            day = JsonSerializer.Parse<DayOfWeek>(@"""wednesday""", options);
            Assert.Equal(DayOfWeek.Wednesday, day);
            day = JsonSerializer.Parse<DayOfWeek>(@"4", options);
            Assert.Equal(DayOfWeek.Thursday, day);

            string json = JsonSerializer.ToString(DayOfWeek.Friday, options);
            Assert.Equal(@"""Friday""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.ToString((DayOfWeek)(-1), options);
            Assert.Equal(@"-1", json);
        }

        public class WhenClass
        {
            public DayOfWeek Day { get; set; }
        }
    }
}
