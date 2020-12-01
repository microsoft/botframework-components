---
category: Skills
subcategory: Samples
language: experimental_skills
title: IT Service Managment (ITSM) Skill
description: IT Service Management Skill Create BusinessRule
---

# {{ page.title }}
The [IT Service Management skill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/experimental/itsmskill) is a 1st Party ServiceNowSkill Template and requires ScriptedRestAPI to be created in ServiceNow Tenant this is a manual step to be done by Tenant Admin below is an example of creating a new RestMessage
to setup a callback url in ServiceNow Tenant:

![RestMessageResource](/Images/RM1.png "Create New RestMessage Resource")
![RestMessageQueryParameters](/Images/RM2.png "Create New RestMessage QueryParameters")

## CodeBlock for RestMessage Resource
```javascript
(function process(/*RESTAPIRequest*/ request, /*RESTAPIResponse*/ response) {
// implement resource here
var name= request.queryParams.name.toString();
var functionName = request.queryParams.postFunctionName.toString();
var parsedData = request.body.data.endPtName;
if(JSUtil.notNil(name)&&JSUtil.notNil(functionName)&&JSUtil.notNil(parsedData))
{
    var restMessage=new GlideRecord('sys_rest_message');
    restMessage.initialize();
    restMessage.name=name;
    restMessage.rest_endpoint=parsedData;
    var restSys_id=restMessage.insert();
    var restHTTPmethod=new GlideRecord('sys_rest_message_fn');
    restHTTPmethod.initialize();
    restHTTPmethod.rest_message=restSys_id;
    restHTTPmethod.function_name=functionName;
    restHTTPmethod.http_method='post';
    restHTTPmethod.rest_endpoint=parsedData;
    restHTTPmethod.HTTPHeaders = {'Content-type':'application/json; charset=utf-8'};

    var sys_id = restHTTPmethod.insert();

    if(JSUtil.notNil(sys_id))
    {
        // Initialize HttpHeader
        var array=[{'name':'Content-type','value':'application/json; charset=utf-8'}];
        var HTTPheader=new GlideRecord('sys_rest_message_fn_headers');
        for(header in array)
        {
            HTTPheader.intialize();
            HTTPheader.rest_message_function=sys_id;
            HTTPheader.name=array[header].name;
            HTTPheader.value=array[header].value;
            HTTPheader.insert();
        }

        return name+' RestMessage created successfully';
    }
    else
    {
        var myError = new sn_ws_err.ServiceError();
        myError.setStatus(410);
        myError.setMessage(sys_id);
        myError.setDetail('error encountered while creating BR');
        return myError;
    }
}
else
{
    return new sn_ws_err.BadRequestError('some mandatory parameters are missing');
}
})(request, response);
```
