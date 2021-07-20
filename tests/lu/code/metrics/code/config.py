class Options:
    def __init__(self):
        """
        Define options
        """
        self.dic = {
            "collect_stat": {
                "enable": True,
                "entity": {
                    "Files": [
                        "EntitiesErrorAnalysisReport.json"
                    ],
                    "LabelSet": set([
                        "ACTION_ASSIGNEE",
                        "ACTION_DUEDATE",
                        "ACTION_PRIORITY",
                        "ACTION_TASKCONTENT",
                        "ACTION_TASKID",                      
                    ]),
                    "LabelKey": "Entity",
                    "PrintLabelOrder": [
                        "ACTION_PRIORITY",
                        "ACTION_TASKCONTENT",
                        "ACTION_TASKID",
                        "ACTION_ASSIGNEE",
                        "ACTION_DUEDATE",
                    ]
                },
                "intent": {
                    "Files": [
                        "ClassificationErrorAnalysisReport-TopScoring.json"
                    ],
                    "LabelSet": set([
                        'ACTION_ADDACTION_LX',
                        'ACTION_DELETEACTION_LX',
                        'ACTION_SHOWACTION_LX',
                        'ACTION_UPDATEACTION_LX',                    
                    ]),
                    "LabelKey": "Class",
                    "PrintLabelOrder": [
                        "ACTION_ADDACTION_LX",
                        "ACTION_DELETEACTION_LX",
                        "ACTION_SHOWACTION_LX",
                        "ACTION_UPDATEACTION_LX"
                    ]
                }
            }
        }
        return
        
    def collect_stat(self) -> bool:
        return self.dic.get("collect_stat", {}).get("enable", False)