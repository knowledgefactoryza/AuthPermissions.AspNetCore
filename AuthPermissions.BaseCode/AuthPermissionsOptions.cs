﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.Configuration;

namespace AuthPermissions.BaseCode
{
    /// <summary>
    /// This contains the options set by the developer and data that is passed between setup extension methods
    /// </summary>
    public class AuthPermissionsOptions
    {

        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// Defaults no using tenants
        /// </summary>
        public TenantTypes TenantType { get; set; }

        /// <summary>
        /// If sharding is turned on, then you can define the sharding database data to use if not 
        /// This defaults to "Default Database", which should be set up to link to the database that also contains the AuthP data
        /// </summary>
        public string ShardingDefaultDatabaseInfoName { get; set; } = "Default Database";

        /// <summary>
        /// This is needed if you are using sharding. Its used to get the ConnectionString
        /// </summary>
        public ConfigurationManager Configuration { get; set; }

        /// <summary>
        /// This turns on the LinkToTenantData feature, e.g. an admin person can access the data in a specific tenant
        /// </summary>
        public LinkToTenantTypes LinkToTenantType { get; set; }

        /// <summary>
        /// This string is used by the <see cref="EncryptDecryptService"/> for services that need to encrypt / decrypt data 
        /// This should be at least 16 characters long
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// When using the "Access the data of other tenant" feature this defines when the link cookie times out.
        /// Defaults to 10 hours.
        /// </summary>
        public int NumMinutesBeforeCookieTimesOut { get; set; } = 600;

        /// <summary>
        /// This will use the Net.RunMethodsSequentially library to safely update / seed a database 
        /// on applications that have mutiple instances using a global lock
        /// </summary>
        public bool UseLocksToUpdateGlobalResources { get; set; } = true;

        /// <summary>
        /// This is used by the Net.RunMethodsSequentially library to lock a folder
        /// If UseRunMethodsSequentially is true, then this property must be filled 
        /// with a path to a directory in your running application 
        /// </summary>
        public string PathToFolderToLock { get; set; }

        /// <summary>
        /// This is where you configure the JwtToken
        /// </summary>
        public AuthPJwtConfiguration ConfigureAuthPJwtToken { get; set; }

        //-------------------------------------------------
        //internal set properties/handles

        /// <summary>
        /// This holds data that is set up during the 
        /// </summary>
        public SetupInternalData InternalData { get; } = new SetupInternalData();

    }
}