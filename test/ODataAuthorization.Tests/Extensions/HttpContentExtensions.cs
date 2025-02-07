﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.


using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ODataAuthorization.Tests.Extensions
{
    /// <summary>
    /// Extensions for HttpContent.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Get the content as the value of ObjectContent.
        /// </summary>
        /// <returns>The content value.</returns>
        public static string AsObjectContentValue(this HttpContent content)
        {
            string json = content.ReadAsStringAsync().Result;
            try
            {
                JObject obj = JsonConvert.DeserializeObject<JObject>(json);
                return obj["value"].ToString();
            }
            catch (JsonReaderException)
            {
            }

            return json;
        }
    }
}
