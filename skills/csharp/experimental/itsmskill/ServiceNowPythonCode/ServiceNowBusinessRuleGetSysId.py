#Need to install requests package for python
import requests

url = 'https://instanceName/api/now/table/sys_script?sysparm_query=name%3DBusinessRuleName&sysparm_limit=1'

# Eg. User name="admin", Password="admin" for this code sample.
# user = 'yourUserName'
# pwd = 'yourPassword'

# Set proper headers
headers = {"Content-Type":"application/json","Accept":"application/json"}

# Do the HTTP request
response = requests.get(url, auth=(user, pwd), headers=headers )

# Check for HTTP codes other than 200
if response.status_code != 200: 
    print('Status:', response.status_code, 'Headers:', response.headers, 'Error Response:',response.json())
    exit()

# Decode the JSON response into a dictionary and use the data
data = response.json()
print(data)