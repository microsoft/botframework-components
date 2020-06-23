using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class FilterCondition
    {
        public string @table { get; set; }
    }

    public class BusinessRule
    {
        public string abort_action { get; set; } = "false";
        public string access { get; set; } = "package_private";
        public string action_delete { get; set; } = "false";
        public string action_insert { get; set; } = "true";
        public string action_query { get; set; } = "false";
        public string action_update { get; set; } = "false";
        public string active { get; set; } = "true";
        public string add_message { get; set; } = "false";
        public string advanced { get; set; } = "true";
        public string change_fields { get; set; } = "true";
        public string client_callable { get; set; } = "false";
        public string collection { get; set; } = "incident";
        public IList<object> condition { get; set; } = new List<object>();
        public IList<object> description { get; set; } = new List<object>();
        public string execute_function { get; set; } = "false";
        public FilterCondition filter_condition { get; set; }
        public string is_rest { get; set; } = "false";
        public IList<object> message { get; set; } = new List<object>();
        public string name { get; set; } = "TestBusinessRuleCreate";
        public string order { get; set; } = "1";
        public string priority { get; set; } = "1";
        public IList<object> rest_method { get; set; } = new List<object>();
        public IList<object> rest_method_text { get; set; } = new List<object>();
        public IList<object> rest_service { get; set; } = new List<object>();
        public IList<object> rest_service_text { get; set; } = new List<object>();
        public IList<object> rest_variables { get; set; } = new List<object>();
        public IList<object> role_conditions { get; set; } = new List<object>();
        public string script { get; set; }
        public string sys_class_name { get; set; } = "sys_script";
        public string sys_created_by { get; set; } = "admin";
        public string sys_created_on { get; set; } = "2020-02-24 17:56:33";
        public string sys_customer_update { get; set; } = "false";
        public string sys_domain { get; set; } = "global";
        public string sys_domain_path { get; set; } = string.Empty;
        public string sys_id { get; set; } = string.Empty;
        public string sys_mod_count { get; set; } = "1";
        public string sys_name { get; set; } = "create br";
        public IList<object> sys_overrides { get; set; } = new List<object>();
        public IList<object> sys_policy { get; set; } = new List<object>();
        public string sys_replace_on_upgrade { get; set; }
        public string sys_update_name { get; set; } = "sys_script_03edd56b2fc38814e8f955f62799b6e8";
        public string sys_updated_by { get; set; } = "admin";
        public string sys_updated_on { get; set; } = "2020-02-24 17:58:22";
        public IList<object> template { get; set; }
        public string when { get; set; } = "after";
    }
}
