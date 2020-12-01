#Set the request parameters
url = 'https://instanceName.service-now.com/api/now/table/sys_script'

#Eg. User name="username", Password="password" for this code sample.
user = 'yourUserName'
pwd = 'yourPassword'

#Set proper headers
headers = {"Content-Type":"application/json","Accept":"application/json"}
# Set Business Rule 
postBusinessRule = "{\r\n      \"abort_action\": \"false\",\r\n      \"access\": \"package_private\",\r\n      \"action_delete\": \"false\",\r\n      \"action_insert\": \"true\",\r\n      \"action_query\": \"false\",\r\n      \"action_update\": \"false\",\r\n      \"active\": \"false\",\r\n      \"add_message\": \"false\",\r\n      \"advanced\": \"true\",\r\n      \"change_fields\": \"true\",\r\n      \"client_callable\": \"false\",\r\n      \"collection\": \"incident\",\r\n      \"condition\": [],\r\n      \"description\": [],\r\n      \"execute_function\": \"false\",\r\n      \"filter_condition\": {\r\n         \"@table\": \"incident\"\r\n      },\r\n      \"is_rest\": \"false\",\r\n      \"message\": [],\r\n      \"name\": \"yourBusinessRuleName\",\r\n      \"order\": \"1\",\r\n      \"priority\": \"1\",\r\n      \"rest_method\": [],\r\n      \"rest_method_text\": [],\r\n      \"rest_service\": [],\r\n      \"rest_service_text\": [],\r\n      \"rest_variables\": [],\r\n      \"role_conditions\": [],\r\n      \"script\": \"(function executeRule(current, previous \/*null when async*\/) {var request = new sn_ws.RESTMessageV2('yourRestAPINameSpace', 'PostApiName'); request.setRequestBody(JSON.stringify()); var response = request.execute(); var responseBody = response.getBody(); var httpStatus = response.getStatusCode();})(current, previous);\",\r\n      \"sys_class_name\": \"sys_script\",\r\n      \"sys_created_by\": \"admin\",\r\n      \"sys_created_on\": \"2020-02-24 17:56:33\",\r\n      \"sys_customer_update\": \"false\",\r\n      \"sys_domain\": \"global\",\r\n      \"sys_domain_path\": \"\/\",\r\n      \"sys_id\": \"\",\r\n      \"sys_mod_count\": \"1\",\r\n      \"sys_name\": \"create test BR\",\r\n      \"sys_overrides\": [],\r\n      \"sys_package\": {\r\n         \"@display_value\": \"Global\",\r\n         \"@source\": \"global\",\r\n         \"#text\": \"global\"\r\n      },\r\n      \"sys_policy\": [],\r\n      \"sys_replace_on_upgrade\": \"false\",\r\n      \"sys_scope\": {\r\n         \"@display_value\": \"Global\",\r\n         \"#text\": \"global\"\r\n      },\r\n      \"sys_update_name\": \"sys_script_03edd56b2fc38814e8f955f62799b6e8\",\r\n      \"sys_updated_by\": \"admin\",\r\n      \"sys_updated_on\": \"2020-02-24 17:58:22\",\r\n      \"template\": [],\r\n      \"when\": \"after\"\r\n   \r\n}"

# Do the HTTP request
response = requests.post(url, auth=(user, pwd), headers=headers ,data=postBusinessRule)
print(response)

# Check for HTTP codes other than 201
if response.status_code == 201: 
    print(response.text) 
    exit()