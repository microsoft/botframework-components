class Options:
    def __init__(self):
        """
        Define options
        """
        self.dic = {
            "bars": {
                "enable": True,
                "intent":
                {
                    "ComparisonType_Thre_Set": set([
                        ('All', None),
                        ('Threshold', 0.5),
                        ('Threshold', 0.8),
                        ('Threshold', 0.9),
                        ('TopScoring', None)
                    ]),
                    "NameSet": set([
                        'ACTION_ADDACTION_LX',
                        'ACTION_DELETEACTION_LX',
                        'ACTION_SHOWACTION_LX',
                        'ACTION_UPDATEACTION_LX',
                        'None'
                    ])
                },
                "entity":
                {
                    "MetricsType_Set": set([
                        "ExtractorsNonStrictMetrics",
                        "ExtractorsStrictMetrics"
                    ]),
                    "NameSet": set([
                        "ACTION_ASSIGNEE",
                        "ACTION_DUEDATE",
                        "ACTION_PRIORITY",
                        "ACTION_TASKCONTENT",
                        "ACTION_TASKID",
                        'None'
                    ])
                },
            },
            "roc": {
                "enable": True,
                "intent":
                {
                    "NameSet": set([
                        'ACTION_ADDACTION_LX',
                        'ACTION_DELETEACTION_LX',
                        'ACTION_SHOWACTION_LX',
                        'ACTION_UPDATEACTION_LX',
                        'None'
                    ]),
                    "x_axis": "Precision",
                    "y_axis": "Recall"
                }
            },
            "confusion": {
                "enable": True,
                "intent":
                {
                    "Files": [
                        "ClassificationErrorAnalysisReport-0.json",
                        "ClassificationErrorAnalysisReport-0.5.json",
                        "ClassificationErrorAnalysisReport-0.8.json",
                        "ClassificationErrorAnalysisReport-0.9.json",
                        "ClassificationErrorAnalysisReport-TopScoring.json"
                    ]
                },
                "entity":
                {
                    "Files": [
                        "EntitiesErrorAnalysisReport.json"
                    ]
                }
            },
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
    
    def plot_bars(self) -> bool:
        return self.dic.get("bars", {}).get("enable", False)
    
    def plot_roc(self) -> bool:
        return self.dic.get("roc", {}).get("enable", False)

    def plot_confusion_matrix(self) -> bool:
        return self.dic.get("confusion", {}).get("enable", False)
    
    def collect_stat(self) -> bool:
        return self.dic.get("collect_stat", {}).get("enable", False)