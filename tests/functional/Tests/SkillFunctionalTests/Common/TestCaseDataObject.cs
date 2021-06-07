// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace SkillFunctionalTests.Common
{
    public class TestCaseDataObject : IXunitSerializable
    {
        private const string TestObjectKey = "TestObjectKey";
        private string _testObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject"/> class.
        /// </summary>
        public TestCaseDataObject()
        {
            // Note: This empty constructor is needed by the serializer.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject"/> class.
        /// </summary>
        /// <param name="testData">An object with the data to be used in the test.</param>
        public TestCaseDataObject(object testData)
        {
            _testObject = JsonConvert.SerializeObject(testData);
        }

        /// <summary>
        /// Used by XUnit.net for deserialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Deserialize(IXunitSerializationInfo serializationInfo)
        {
            _testObject = serializationInfo.GetValue<string>(TestObjectKey);
        }

        /// <summary>
        /// Used by XUnit.net for serialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Serialize(IXunitSerializationInfo serializationInfo)
        {
            serializationInfo.AddValue(TestObjectKey, _testObject);
        }

        /// <summary>
        /// Gets the test data object for the specified .Net type.
        /// </summary>
        /// <typeparam name="T">The type of the object to be returned.</typeparam>
        /// <returns>The test object instance.</returns>
        public T GetObject<T>()
        {
            return JsonConvert.DeserializeObject<T>(_testObject);
        }

        public override string ToString()
        {
            try
            {
                var testCase = GetObject<TestCase>();
                return testCase.Description;
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }
    }
}
