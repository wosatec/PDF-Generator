using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PDF_POC.Core.Json
{
    public class JsonAdvancedElement<TData>
    {
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly JsonElement _data;

        public TData Value { get => _data.Deserialize<TData>(_options); }

        public JsonAdvancedElement(string plainJson)
        {
            _data = JsonSerializer.Deserialize<JsonElement>(plainJson, _options);

            // Throws an exception, when the provided JSON is not compatible to TData.
            _ = Value;
        }

        public JsonAdvancedElement(JsonElement data)
        {
            _data = data;

            // Throws an exception, when the provided JSON is not compatible to TData.
            _ = Value;
        }

        public TSubData Deserialize<TSubData>()
            where TSubData : TData
        {
            return _data.Deserialize<TSubData>(_options);
        }

        public JsonAdvancedElement<TSubData> FindNode<TSubData>(string keyPath)
        {
            JsonElement element = FindData(keyPath);

            return new JsonAdvancedElement<TSubData>(element);
        }

        public IEnumerable<JsonAdvancedElement<TSubData>> FindNodeArray<TSubData>(string keyPath)
        {
            JsonElement element = FindData(keyPath);

            return element.ValueKind switch
            {
                JsonValueKind.Null => new JsonAdvancedElement<TSubData>[] { null },
                JsonValueKind.Object => new[] { new JsonAdvancedElement<TSubData>(element) },
                JsonValueKind.Array => element.EnumerateArray().Select(line => new JsonAdvancedElement<TSubData>(line)).ToList(),
                _ => throw new InvalidOperationException("unsupported JsonValueKind " + element.ValueKind),
            };
        }

        public TSubData FindData<TSubData>(string keyPath)
        {
            JsonElement element = FindData(keyPath);

            return element.Deserialize<TSubData>(_options);
        }

        public IEnumerable<TSubData> FindDataArray<TSubData>(string keyPath)
        {
            JsonElement content = FindData(keyPath);

            return content.Deserialize<IEnumerable<TSubData>>(_options);
        }

        public string FindString(string keyPath)
        {
            return FindData<string>(keyPath);
        }

        public IEnumerable<string> FindStringArray(string keyPath)
        {
            JsonElement content = FindData(keyPath);

            return content.ValueKind switch
            {
                JsonValueKind.Null => new string[] { null },
                JsonValueKind.String => new[] { content.GetString() },
                JsonValueKind.Array => content.EnumerateArray().Select(line => line.ToString()).ToList(),
                _ => throw new InvalidOperationException("unsupported JsonValueKind " + content.ValueKind),
            };
        }

        private JsonElement FindData(string keyPath)
        {
            JsonElement element = _data;

            try
            {
                string[] keys = keyPath.Split('.');

                foreach (string key in keys)
                {
                    element = GetElement(element, key);
                }

                return element;
            }
            catch (Exception exception)
            {
                Console.WriteLine(keyPath + ": " + exception.ToString());
                throw;
            }
        }

        private static JsonElement GetElement(JsonElement element, string key)
        {
            if (key.Contains("[") && key.EndsWith("]"))
            {
                string[] tokens = key.Split("[");

                string keyName = tokens[0];

                string[] indexTokens = tokens[1].Split("]");

                string index = indexTokens[0];

                JsonElement arrayElement = element.GetProperty(keyName);

                if (arrayElement.ValueKind == JsonValueKind.Array
                    && int.TryParse(index, out int indexValue))
                {
                    return GetArrayValue(arrayElement, indexValue);
                }
                else if (arrayElement.TryGetProperty(index, out JsonElement property))
                {
                    return property;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid index {index} for property {keyName}");
                }
            }

            return element.GetProperty(key);
        }

        private static JsonElement GetArrayValue(JsonElement arrayElement, int indexValue)
        {
            if (indexValue >= 0 && indexValue < arrayElement.GetArrayLength())
            {
                foreach ((JsonElement arrayItem, int itemIndex) in arrayElement.EnumerateArray().Select((item, index) => (item, index)))
                {
                    if (itemIndex == indexValue)
                    {
                        return arrayItem;
                    }
                }
            }

            throw new InvalidOperationException($"index was {indexValue}, but {arrayElement.GetArrayLength()} elements were present");
        }
    }
}
