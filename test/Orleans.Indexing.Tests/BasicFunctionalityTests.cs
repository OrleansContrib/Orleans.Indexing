using System;
using Orleans.Indexing.Facet;
using Xunit;
using System.Linq;

namespace Orleans.Indexing.Tests
{
    public class BasicFunctionalityTests
    {
        private const string IntNullValue = "111";
        private const string UIntNullValue = "222";
        private const string FloatNullValue = "333";
        private const string DoubleNullValue = "444";
        private const string DecimalNullValue = "555";
        private const string DateTimeNullValue = "2018-06-05T11:36:26.9047468-07:00";

        class NullValuesTestState
        {
            [Index(NullValue = IntNullValue)]
            public int IntVal { get; set; }
            public int? NIntVal { get; set; }

            [Index(NullValue = UIntNullValue)]
            public uint UintVal { get; set; }
            public uint? NUintVal { get; set; }

            [Index(NullValue = FloatNullValue)]
            public float FloatVal { get; set; }
            public float? NFloatVal { get; set; }

            [Index(NullValue = DoubleNullValue)]
            public double DoubleVal { get; set; }
            public double? NDoubleVal { get; set; }

            [Index(NullValue = DecimalNullValue)]
            public decimal DecimalVal { get; set; }
            public decimal? NDecimalVal { get; set; }

            [Index(NullValue = DateTimeNullValue)]
            public DateTime DatetimeVal { get; set; }
            public DateTime? NDatetimeVal { get; set; }

            public string StringVal { get; set; }
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public void SetNullValuesTest()
        {
            var state = new IndexedGrainStateWrapper<NullValuesTestState>();
            state.EnsureNullValues(IndexRegistry.EmptyPropertyNullValues);
            Assert.Equal(int.Parse(IntNullValue), state.UserState.IntVal);
            Assert.Null(state.UserState.NIntVal);
            Assert.Equal(uint.Parse(UIntNullValue), state.UserState.UintVal);
            Assert.Null(state.UserState.NUintVal);
            Assert.Equal(float.Parse(FloatNullValue), state.UserState.FloatVal);
            Assert.Null(state.UserState.NFloatVal);
            Assert.Equal(double.Parse(DoubleNullValue), state.UserState.DoubleVal);
            Assert.Null(state.UserState.NDoubleVal);
            Assert.Equal(DateTime.Parse(DateTimeNullValue), state.UserState.DatetimeVal);
            Assert.Null(state.UserState.NDatetimeVal);

            Assert.Null(state.UserState.StringVal);
        }

        /// <summary>
        /// Validates indexes without having to load them into a Silo.
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public void Test_Validate_Indexes()
            => IndexValidator.Validate(typeof(BasicFunctionalityTests).Assembly.GetIndexedGrainClasses().ToArray());
    }
}
