﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class HierarchicalTenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantFullName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantName { get; set; }

        public string DataKey { get; set; }

        //-------------------------------------------
        //used for Create and Move

        public List<KeyValuePair<int,string>> ListOfTenants { get; private set; }

        public int ParentId { get; set; }

        public static IQueryable<HierarchicalTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new HierarchicalTenantDto
            {
                TenantId = x.TenantId,
                TenantFullName = x.TenantFullName,
                TenantName = x.GetTenantName(),
                DataKey = x.GetTenantDataKey()
            });
        }

        public static async Task<HierarchicalTenantDto> SetupForCreateAsync(IAuthTenantAdminService tenantAdminService)
        {
            var result = new HierarchicalTenantDto
            {
                ListOfTenants = await tenantAdminService.QueryTenants()
                    .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                    .ToListAsync()
            };
            result.ListOfTenants.Insert(0, new KeyValuePair<int, string>(0, "< none >"));

            return result;
        }

        public static async Task<HierarchicalTenantDto> SetupForMoveAsync(Tenant tenant, IAuthTenantAdminService tenantAdminService)
        {
            var result = new HierarchicalTenantDto
            {
                TenantId = tenant.TenantId,
                TenantFullName = tenant.TenantFullName,
                TenantName = tenant.GetTenantName(),

                ListOfTenants = await tenantAdminService.QueryTenants()
                    .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                    .ToListAsync(),
                ParentId = tenant.ParentTenantId ?? 0
            };
            result.ListOfTenants.Insert(0, new KeyValuePair<int, string>(0, "< none >"));

            return result;
        }

        public static HierarchicalTenantDto SetupForEdit(Tenant tenant)
        {
            return new HierarchicalTenantDto
            {
                TenantId = tenant.TenantId,
                TenantFullName = tenant.TenantFullName,
                TenantName = tenant.GetTenantName()
            };
        }

        public static async Task<HierarchicalTenantDto> SetupForDeleteAsync(Tenant tenant, IAuthTenantAdminService tenantAdminService)
        {
            return new HierarchicalTenantDto
            {
                TenantId = tenant.TenantId,
                TenantFullName = tenant.TenantFullName,
                TenantName = tenant.GetTenantName(), 
                ListOfTenants = (await tenantAdminService.GetHierarchicalTenantChildrenViaIdAsync(tenant.TenantId))
                    .Select(x => new KeyValuePair<int,string>(x.TenantId, x.TenantFullName) )
                    .ToList()
            };
        }
    }
}