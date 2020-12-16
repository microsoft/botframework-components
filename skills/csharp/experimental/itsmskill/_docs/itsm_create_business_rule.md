---
category: Skills
subcategory: Samples
language: experimental_skills
title: IT Service Managment (ITSM) Skill
description: IT Service Management Skill Create BusinessRule
---

# {{ page.title }}
The [IT Service Management skill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/experimental/itsmskill) is a 1st Party ServiceNowSkill Template and requires ScriptedRestAPI to be created in ServiceNow Tenant this is a manual step to be done by Tenant Admin below is an example of creating a new BusinessRule
to setup when Incident changes in ServiceNow Tenant:
![BusinessRuleResource](/Images/BR1.png "Create New BusinessRule Resource")
![BusinessRuleQueryParameters](/Images/BR2.png "Create New BusinessRule QueryParameters")

## CodeBlock for BusinessRuleResource
```javascript
(function process(/*RESTAPIRequest*/ request, /*RESTAPIResponse*/ response) {
// implement resource here
var name= request.queryParams.name.toString();
var whenToRun = request.queryParams.whenToRun.toString();
var tableName = request.queryParams.tableName.toString();
var action = request.queryParams.action.toString();
var filterCondition = request.queryParams.filterCondition.toString();
var advance = request.queryParams.advance.toString();
var parsedData = request.body.data.script;
if(JSUtil.notNil(name)&&JSUtil.notNil(whenToRun)&&JSUtil.notNil(tableName)&&JSUtil.notNil(action)&&JSUtil.notNil(filterCondition)&&JSUtil.notNil(advance))
{
    var Br=new GlideRecord('sys_script');
    Br.intialize();
    Br.name= name;
    Br.collection=tableName;
    Br.advance=true;
    Br.when='after';
    Br.action_update=true;
    Br.action_insert=true;
    Br.action_delete=true;
    Br.active= true;
    Br.filter_condition='urgencyVALCHANGES^ORdescriptionVALCHANGES^ORpriorityVALCHANGES^ORdescriptionVALCHANGES^ORassigned_toVALCHANGES^EQ';
    Br.script = parsedData;
    var sys_id = Br.insert();
    if(JSUtil.notNil(sys_id))
    {
        return name+' BR created successfully';
    }
    else
    {
        var myError = new sn_ws_err.ServiceError();
        myError.setStatus(410);
        myError.setMessage('Error');
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
