﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.PermissionsCode.Services;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore
{
    /// <summary>
    /// A set of extension methods for creation and configuring the AuthPermissions that uses ASP.NET Core features
    /// </summary>
    public static class SetupExtensions
    {
        /// <summary>
        /// This registers the code to add AuthP's claims using IndividualAccounts
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAuthentication(this AuthSetupData setupData)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.IndividualAccounts;
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims<IdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This registers the code to add AuthP's claims using IndividualAccounts that has a custom Identity User
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAuthentication<TCustomIdentityUser>(this AuthSetupData setupData)
            where TCustomIdentityUser : IdentityUser
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.IndividualAccounts;
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<TCustomIdentityUser>, AddPermissionsToUserClaims<TCustomIdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This registers an OpenIDConnect set up to work with Azure AD authorization
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="eventSettings">This contains the data needed to add the AuthP claims to the Azure AD login</param>
        /// <returns></returns>
        public static AuthSetupData AzureAdAuthentication(this AuthSetupData setupData, AzureAdEventSettings eventSettings)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.OpenId;
            setupData.Services.SetupOpenAzureAdOpenId(eventSettings);

            return setupData;
        }

        /// <summary>
        /// This says you have manually set up the Authentication code which adds the AuthP Roles and Tenant claims to the cookie or JWT Token
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData ManualSetupOfAuthentication(this AuthSetupData setupData)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.UserProvidedAuthentication;

            return setupData;
        }

        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts identity system using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData AddSuperUserToIndividualAccounts(this AuthSetupData setupData)
        {
            setupData.CheckAuthorizationIsIndividualAccounts();
            setupData.Options.InternalData.RunSequentiallyOptions
                .RegisterServiceToRunInJob<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts (with custom identity)using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData AddSuperUserToIndividualAccounts<TCustomIdentityUser>(this AuthSetupData setupData)
            where TCustomIdentityUser : IdentityUser, new()
        {
            setupData.CheckAuthorizationIsIndividualAccounts();
            setupData.Options.InternalData.RunSequentiallyOptions
                .RegisterServiceToRunInJob<StartupServiceIndividualAccountsAddSuperUser<TCustomIdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This allows you to replace the default <see cref="ShardingConnections"/> code with you own code.
        /// This allows you to add you own approach to managing sharding databases
        /// NOTE: The <see cref="IOptionsSnapshot{TOptions}"/> of the connection strings and the shardingsettings.json file are still registered
        /// </summary>
        /// <typeparam name="TYourShardingCode">Your class that implements the <see cref="IShardingConnections"/> interface.</typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        /// <exception cref="AuthPermissionsException"></exception>
        public static AuthSetupData ReplaceShardingConnections<TYourShardingCode>(this AuthSetupData setupData)
            where TYourShardingCode : class, IShardingConnections
        {
            if (!setupData.Options.TenantType.IsSharding())
                throw new AuthPermissionsException(
                    $"The sharding feature isn't turned on so you can't override the {nameof(ShardingConnections)} service.");

            setupData.Services.AddScoped<IShardingConnections, TYourShardingCode>();
            setupData.Options.InternalData.OverrideShardingConnections = true;

            return setupData;
        }

        /// <summary>
        /// This allows you to register your implementation of the <see cref="IShardingSelectDatabase"/> service.
        /// This service is used in the "sign up" feature in the SupportCode part, or if you want to use this in your own code.
        /// </summary>
        /// <typeparam name="TGetDatabase">Your class that implements the <see cref="IShardingSelectDatabase"/> interface.</typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        /// <exception cref="AuthPermissionsException"></exception>
        public static AuthSetupData RegisterShardingGetDatabase<TGetDatabase>(this AuthSetupData setupData)
            where TGetDatabase : class, IShardingSelectDatabase
        {
            if (!setupData.Options.TenantType.IsSharding())
                throw new AuthPermissionsException(
                    $"The sharding feature isn't turned on so adding your {nameof(IShardingSelectDatabase)} service isn't useful.");

            setupData.Services.AddScoped<IShardingSelectDatabase, TGetDatabase>();

            return setupData;
        }

        /// <summary>
        /// This will finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// NOTE: It assumes the AuthPermissions database has been created and has the current migration applied
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAspNetCorePart(this AuthSetupData setupData)
        {
            setupData.RegisterCommonServices();
        }

        /// <summary>
        /// This finalizes the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// This may trigger code to run on startup, before ASP.NET Core active, to
        /// 1) Migrate the AuthP's database
        /// 2) Run a bulk load process
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="optionsAction">You can your own startup services by adding them to the <see cref="RunSequentiallyOptions"/> options.
        /// Your startup services will be registered after the Migrate the AuthP's database and bulk load process, so set the OrderNum in
        /// your startup services to a negative to get them before the AuthP startup services</param>
        public static void SetupAspNetCoreAndDatabase(this AuthSetupData setupData,
            Action<RunSequentiallyOptions> optionsAction = null)
        {
            setupData.CheckDatabaseTypeIsSet();

            setupData.RegisterCommonServices();

            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.SqliteInMemory)
                //Only run the migration on the AuthP's database if its not a in-memory database
                setupData.Options.InternalData.RunSequentiallyOptions
                    .RegisterServiceToRunInJob<StartupServiceMigrateAuthPDatabase>();

            if (!(setupData.Options.InternalData.RolesPermissionsSetupData == null || !setupData.Options.InternalData.RolesPermissionsSetupData.Any()) ||
                !(setupData.Options.InternalData.TenantSetupData == null || !setupData.Options.InternalData.TenantSetupData.Any()) ||
                !(setupData.Options.InternalData.UserRolesSetupData == null || !setupData.Options.InternalData.UserRolesSetupData.Any()))
                //Only run this if there is some Bulk Load data to apply
                setupData.Options.InternalData.RunSequentiallyOptions
                    .RegisterServiceToRunInJob<StartupServiceBulkLoadAuthPInfo>();

            optionsAction?.Invoke(setupData.Options.InternalData.RunSequentiallyOptions);
        }

        /// <summary>
        /// This will set up the basic AppPermissions parts and and any roles, tenants and users in the in-memory database
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns>The built ServiceProvider for access to AuthP's services</returns>
        public static async Task<ServiceProvider> SetupForUnitTestingAsync(this AuthSetupData setupData)
        {
            setupData.CheckDatabaseTypeIsSetToSqliteInMemory();

            setupData.RegisterCommonServices();

            var serviceProvider = setupData.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            context.Database.EnsureCreated();

            var findUserIdService = serviceProvider.GetService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(setupData.Options, findUserIdService);

            status.IfErrorsTurnToException();

            return serviceProvider;
        }


        //------------------------------------------------
        // private methods

        private static void RegisterCommonServices(this AuthSetupData setupData)
        {
            //common tests
            setupData.CheckThatAuthorizationTypeIsSetIfNotInUnitTestMode();

            //AuthP services
            setupData.Services.AddSingleton(setupData.Options);
            setupData.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            setupData.Services.AddSingleton<IAuthorizationHandler, PermissionPolicyHandler>();
            setupData.Services.AddScoped<IClaimsCalculator, ClaimsCalculator>();
            setupData.Services.AddTransient<IUsersPermissionsService, UsersPermissionsService>();
            setupData.Services.AddTransient<IEncryptDecryptService, EncryptDecryptService>();
            if (setupData.Options.TenantType.IsMultiTenant())
                SetupMultiTenantServices(setupData);

            //The factories for the optional services
            setupData.Services.AddTransient<IAuthPServiceFactory<ISyncAuthenticationUsers>, SyncAuthenticationUsersFactory>();
            setupData.Services.AddTransient<IAuthPServiceFactory<IFindUserInfoService>, FindUserInfoServiceFactory>();
            setupData.Services.AddTransient<IAuthPServiceFactory<ITenantChangeService>, TenantChangeServiceFactory>();

            //Admin services
            setupData.Services.AddTransient<IAuthRolesAdminService, AuthRolesAdminService>();
            setupData.Services.AddTransient<IAuthTenantAdminService, AuthTenantAdminService>();
            setupData.Services.AddTransient<IAuthUsersAdminService, AuthUsersAdminService>();
            setupData.Services.AddTransient<IBulkLoadRolesService, BulkLoadRolesService>();
            setupData.Services.AddTransient<IBulkLoadTenantsService, BulkLoadTenantsService>();
            setupData.Services.AddTransient<IBulkLoadUsersService, BulkLoadUsersService>();

            //Other services
            setupData.Services.AddTransient<IDisableJwtRefreshToken, DisableJwtRefreshToken>();
            if (setupData.Options.ConfigureAuthPJwtToken != null)
            {
                //The user is using AuthP's TokenBuilder

                setupData.Options.ConfigureAuthPJwtToken.CheckThisJwtConfiguration()
                    .IfErrorsTurnToException();
                setupData.Services.AddTransient<ITokenBuilder, TokenBuilder>();
            }
        }

        private static void SetupMultiTenantServices(AuthSetupData setupData)
        {
            //This sets up the code to get the DataKey to the application's DbContext

            //Check the TenantType and LinkToTenantType for incorrect versions
            if (!setupData.Options.TenantType.IsHierarchical()
                && setupData.Options.LinkToTenantType == LinkToTenantTypes.AppAndHierarchicalUsers)
                throw new AuthPermissionsException(
                    $"You can't set the {nameof(AuthPermissionsOptions.LinkToTenantType)} to " +
                    $"{nameof(LinkToTenantTypes.AppAndHierarchicalUsers)} unless you are using AuthP's hierarchical multi-tenant setup.");

            //The "Access the data of other tenant" feature is turned on so register the services

            //And register the service that manages the cookie and the service to start/stop linking
            setupData.Services.AddScoped<IAccessTenantDataCookie, AccessTenantDataCookie>();
            setupData.Services.AddScoped<ILinkToTenantDataService, LinkToTenantDataService>();
            if (setupData.Options.TenantType.IsSharding())
            {
                if (setupData.Options.Configuration == null)
                    throw new AuthPermissionsException(
                        $"You must set the {nameof(AuthPermissionsOptions.Configuration)} to the ASP.NET Core Configuration when using Sharding");

                //This gets access to the ConnectionStrings
                setupData.Services.Configure<ConnectionStringsOption>(setupData.Options.Configuration.GetSection("ConnectionStrings"));
                //This gets access to the ShardingData in the separate shardingsettings.json file
                setupData.Services.Configure<ShardingSettingsOption>(setupData.Options.Configuration);
                //This adds the shardingsettings.json to the configuration
                setupData.Options.Configuration.AddJsonFile("shardingsettings.json", optional: true, reloadOnChange: true);

                if (!setupData.Options.InternalData.OverrideShardingConnections)
                    //Don't add the default service if the developer has added their own service
                    setupData.Services.AddScoped<IShardingConnections, ShardingConnections>();
                setupData.Services.AddScoped<ILinkToTenantDataService, LinkToTenantDataService>();

                switch (setupData.Options.LinkToTenantType)
                {
                    case LinkToTenantTypes.OnlyAppUsers:
                        setupData.Services
                            .AddScoped<IGetShardingDataFromUser, GetShardingDataUserAccessTenantData>();
                        break;
                    case LinkToTenantTypes.AppAndHierarchicalUsers:
                        setupData.Services
                            .AddScoped<IGetShardingDataFromUser,
                                GetShardingDataAppAndHierarchicalUsersAccessTenantData>();
                        break;
                    default:
                        setupData.Services.AddScoped<IGetShardingDataFromUser, GetShardingDataUserNormal>();
                        break;
                }
            }
            else
            {
                setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromUserNormal>();

                switch (setupData.Options.LinkToTenantType)
                {
                    case LinkToTenantTypes.OnlyAppUsers:
                        setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromAppUserAccessTenantData>();
                        break;
                    case LinkToTenantTypes.AppAndHierarchicalUsers:
                        setupData.Services
                            .AddScoped<IGetDataKeyFromUser, GetDataKeyFromAppAndHierarchicalUsersAccessTenantData>();
                        break;
                    default:
                        setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromUserNormal>();
                        break;
                }
            }
        }
    }
}