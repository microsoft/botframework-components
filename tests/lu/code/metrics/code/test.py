import unittest, os

class TestPlot(unittest.TestCase):
    def __init__(self, *args, **kwargs):
        super(TestPlot, self).__init__(*args, **kwargs)
        
        self.output_dir = os.path.join(
            os.path.dirname(os.path.dirname(__file__)), 'data', 'unittest')
        if os.path.exists(self.output_dir) is False: os.mkdir(self.output_dir)
        return

    def test_plot_bar_data(self):
        '''
        usage: python -m unittest code.test.TestPlot.test_plot_bar_data
        '''
        from metrics.code.plot_func import BarData, plot_bar_data

        bd = BarData()

        '''
        bd.data = {
            'Groups': ['G1', 'G2', 'G3', 'G4', 'G5', 'G6', 'G7', 'G8', 'G9'],

            'Precision': {
                'BarA': [1]*9,
                'BarB': [2]*9,
            },

            'Recall': {
                'BarA': [3]*9,
                'BarB': [4]*9,
            },

            'F1': {
                'BarA': [5]*9,
                'BarB': [6]*9,
            }
        }
        '''

        '''
        N_groups = 4
        groups = []
        for g in range(N_groups):
            groups.append('G%d'%(g+1))

        N_curves = 4
        curves = {}
        for c in range(N_curves):
            curves['Curve%d'%(c+1)] = [c+1]*N_groups

        bd.data = {
            'Groups': groups,
            'Precision': curves,
            'Recall': curves,
            'F1': curves
        }
        '''
        
        #'''
        bd.data = \
        {
            'Groups': ['ACTION_ADDACTION_LX', 'ACTION_DELETEACTION_LX', 'ACTION_SHOWACTION_LX', 'ACTION_UPDATEACTION_LX'],
            'Precision': {
                'res= cmp=All thre=None': [0.2702702702702703, 0.12612612612612611, 0.2702702702702703, 0.25225225225225223],
                'res= cmp=Threshold thre=0.5': [0.5142857142857142, 0.8, 0.8181818181818182, 0.6666666666666666],
                'res= cmp=Threshold thre=0.8': [0.8235294117647058, 0.0, 0.8571428571428571, 0.4],
                'res= cmp=Threshold thre=0.9': [0.8571428571428571, 0.0, 0.9090909090909091, 0.0]},
            'Recall': {
                'res= cmp=All thre=None': [1.0, 1.0, 1.0, 1.0],
                'res= cmp=Threshold thre=0.5': [0.6, 0.2857142857142857, 0.6, 0.2857142857142857],
                'res= cmp=Threshold thre=0.8': [0.4666666666666667, 0.0, 0.4, 0.07142857142857142],
                'res= cmp=Threshold thre=0.9': [0.4, 0.0, 0.3333333333333333, 0.0]},
            'F1': {
                'res= cmp=All thre=None': [0.4255319148936171, 0.22399999999999998, 0.4255319148936171, 0.40287769784172656],
                'res= cmp=Threshold thre=0.5': [0.5538461538461538, 0.4210526315789473, 0.6923076923076923, 0.4],
                'res= cmp=Threshold thre=0.8': [0.5957446808510638, 0.0, 0.5454545454545455, 0.12121212121212122],
                'res= cmp=Threshold thre=0.9': [0.5454545454545455, 0.0, 0.4878048780487804, 0.0]}
        }    
        #'''    

        plot_bar_data(
            bd,
            os.path.join(
                self.output_dir,'test_plot_bar_data.png')
        )
        return

    def test_plot_roc_data(self):
        '''
        usage: python -m unittest code.test.TestPlot.test_plot_roc_data
        '''
        from metrics.code.plot_func import RocData, plot_roc_data

        rd = RocData()

        rd.data = \
            {
                'add_action': {
                    "train_as_test": {
                        'x': [0, 1, 1],
                        'y': [1, 1, 0],
                    },
                    "test_as_test": {
                        'x': [0, 0.5, 0.8],
                        'y': [0.8, 0.5, 0]
                    }
                },
                'del_action': {
                    "train_as_test": {
                        'x': [0, 1, 1],
                        'y': [1, 1, 0],
                    },
                    "test_as_test": {
                        'x': [0, 0.5, 0.8],
                        'y': [0.8, 0.5, 0]
                    }
                },
                'update_action': {
                    "train_as_test": {
                        'x': [0, 1, 1],
                        'y': [1, 1, 0],
                    },
                    "test_as_test": {
                        'x': [0, 0.5, 0.8],
                        'y': [0.8, 0.5, 0]
                    }
                }
            }
        
        plot_roc_data(
            rd,
            os.path.join(
                self.output_dir, 'test_plot_roc_data.png')
        )
        return
    
    def test_plot_confusion_matrix_data(self):
        '''
        usage: python -m unittest code.test.TestPlot.test_plot_confusion_matrix_data
        '''
        from metrics.code.plot_func import ConfusionMatrixData, plot_confusion_matrix_data
        import numpy as np

        cd = ConfusionMatrixData()

        cd.data = \
            {
                'train_as_test, intent, thre=0.5': {
                    'row_labels': ['add_action', 'update_action'],
                    'col_labels': ['add_action', 'update_action', 'None'],
                    'matrix': np.array(
                        [
                            [10, 5, 1],
                            [5, 11, 1],
                        ]
                    )
                },
                'train_as_test, intent, thre=0.9': {
                    'row_labels': ['add_action', 'update_action'],
                    'col_labels': ['add_action', 'update_action', 'None'],
                    'matrix': np.array(
                        [
                            [15, 0, 1],
                            [0, 16, 1],
                        ]
                    )
                },
                'test_as_test, intent, thre=0.9': {
                    'row_labels': ['add_action', 'update_action'],
                    'col_labels': ['add_action', 'update_action', 'None'],
                    'matrix': np.array(
                        [
                            [16, 0, 0],
                            [0, 17, 0],
                        ]
                    )
                }
            }
        
        plot_confusion_matrix_data(
            cd,
            os.path.join(
                self.output_dir, 'test_plot_confusion_matrix_data.png'
            )
        )
        return

class TestBarData(unittest.TestCase):    
    def test_add_ClassifierMetrics(self):
        '''
        usage: python -m unittest code.test.TestBarData.test_add_ClassifierMetrics
        '''
        from metrics.code.plot_func import BarData
        import pdb

        json_obj_0 = \
        [
            {
                "ComparisonType": "All",
                "Threshold": None,
                "ClassifiersMetrics": [
                    {
                        "Name": "ACTION_ADDACTION_LX",
                        "Scores": {
                            "Precision": 0.1,
                            "Recall": 0.2,
                            "F1": 0.3
                        }
                    },
                    {
                        "Name": "ACTION_UPDATEACTION_LX",
                        "Scores": {
                            "Precision": 0.4,
                            "Recall": 0.5,
                            "F1": 0.6
                        }
                    }
                ]
            },
            {
                "ComparisonType": "Threshold",
                "Threshold": 0.5,
                "ClassifiersMetrics": [
                    {
                        "Name": "ACTION_ADDACTION_LX",
                        "Scores": {
                            "Precision": 0.7,
                            "Recall": 0.8,
                            "F1": 0.9
                        }
                    },
                    {
                        "Name": "ACTION_UPDATEACTION_LX",
                        "Scores": {
                            "Precision": 1.0,
                            "Recall": 1.1,
                            "F1": 1.2
                        }
                    }
                ]
            },            
        ]

        json_obj_1 = \
        [
            {
                "ComparisonType": "All",
                "Threshold": None,
                "ClassifiersMetrics": [
                    {
                        "Name": "ACTION_ADDACTION_LX",
                        "Scores": {
                            "Precision": 0.15,
                            "Recall": 0.25,
                            "F1": 0.35
                        }
                    },
                    {
                        "Name": "ACTION_UPDATEACTION_LX",
                        "Scores": {
                            "Precision": 0.45,
                            "Recall": 0.55,
                            "F1": 0.65
                        }
                    }
                ]
            },
            {
                "ComparisonType": "Threshold",
                "Threshold": 0.5,
                "ClassifiersMetrics": [
                    {
                        "Name": "ACTION_ADDACTION_LX",
                        "Scores": {
                            "Precision": 0.75,
                            "Recall": 0.85,
                            "F1": 0.95
                        }
                    },
                    {
                        "Name": "ACTION_UPDATEACTION_LX",
                        "Scores": {
                            "Precision": 1.05,
                            "Recall": 1.15,
                            "F1": 1.25
                        }
                    }
                ]
            }, 
        ]

        bd = BarData()        
        bd.add_ClassifierMetrics(
            json_obj_0, 'obj_0', {('All', None), ('Threshold', 0.5)}, set(['ACTION_ADDACTION_LX', 'ACTION_UPDATEACTION_LX']))
        bd.add_ClassifierMetrics(
            json_obj_1, 'obj_1', {('All', None), ('Threshold', 0.5)}, set(['ACTION_ADDACTION_LX', 'ACTION_UPDATEACTION_LX']))
        
        ans = \
            {
                'Groups': ['ACTION_ADDACTION_LX', 'ACTION_UPDATEACTION_LX'],
                'Precision': {
                    'res=obj_0 cmp=All thre=None': [0.1, 0.4],
                    'res=obj_0 cmp=Threshold thre=0.5': [0.7, 1.0],
                    'res=obj_1 cmp=All thre=None': [0.15, 0.45],
                    'res=obj_1 cmp=Threshold thre=0.5': [0.75, 1.05]}, 
                'Recall': {
                    'res=obj_0 cmp=All thre=None': [0.2, 0.5],
                    'res=obj_0 cmp=Threshold thre=0.5': [0.8, 1.1],
                    'res=obj_1 cmp=All thre=None': [0.25, 0.55],
                    'res=obj_1 cmp=Threshold thre=0.5': [0.85, 1.15]}, 
                'F1': {
                    'res=obj_0 cmp=All thre=None': [0.3, 0.6],
                    'res=obj_0 cmp=Threshold thre=0.5': [0.9, 1.2], 
                    'res=obj_1 cmp=All thre=None': [0.35, 0.65],
                    'res=obj_1 cmp=Threshold thre=0.5': [0.95, 1.25]}
            }
        assert bd.data == ans 
        return

class TestConfusionMatrixData(unittest.TestCase):    
    def test_add_ClassifierErrorAnalysisReport(self):
        '''
        usage: python -m unittest code.test.TestConfusionMatrixData.test_add_ClassifierErrorAnalysisReport
        '''
        from metrics.code.plot_func import ConfusionMatrixData
        import numpy as np

        cd = ConfusionMatrixData()

        desc1 = "fake train_as_test, intent, thre=0.5"
        annot_data1 = [
            # doc id 0
            {
                "text": "add task",
                "intent": "add"
            },
            # doc id 1
            {
                "text": "update task",
                "intent": "update"
            }
        ]
        res_data1 = [
            {
                "DocumentId": "0",
                "ConfusionType": "TruePositive",
                "Class": "add",
                "Score": 0.9,
                "Text": "add task"
            },
            {
                "DocumentId": "0",
                "ConfusionType": "FalsePositive",
                "Class": "update",
                "Score": 0.6,
                "Text": "add task"
            },    
            {
                "DocumentId": "1",
                "ConfusionType": "FalseNegative",
                "Class": "update",
                "Score": None,
                "Text": "update task"
            },    
            {
                "DocumentId": "1",
                "ConfusionType": "FalsePositive",
                "Class": "add",
                "Score": 0.6,
                "Text": "update task"
            },
        ]
        
        cd.add_ClassifierErrorAnalysisReport(
            desc1,
            annot_data1,
            res_data1
        )
        
        ans = {
            'fake train_as_test, intent, thre=0.5':
            {
                'row_labels': ['add', 'update'],
                'col_labels': ['add', 'update', 'None'],
                'matrix': np.array(
                    [
                        [1, 1, 0],
                        [1, 0, 1]
                    ]
                )
            }
        }
        
        for k in ans:
            assert k in cd.data 
            assert ans[k]['row_labels'] == cd.data[k]['row_labels']
            assert ans[k]['col_labels'] == cd.data[k]['col_labels']
            assert np.array_equal(ans[k]['matrix'], cd.data[k]['matrix'])
            
        return

class TestStatData(unittest.TestCase):    
    def test_add_json_obj(self):
        '''
        usage: python -m unittest code.test.TestStatData.test_add_json_obj
        '''
        from metrics.code.collect_stat_func import StatData
        import numpy as np
        import pdb

        sd = StatData()

        json_obj = \
                [
                    {
                        "ConfusionType": "TruePositive", # x1
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "FalsePositive", # x2
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "FalsePositive",
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "FalseNegative", # x3
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "FalseNegative",
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "FalseNegative",
                        "Class": "A",
                    },
                    {
                        "ConfusionType": "TruePositive", # x3
                        "Class": "B",
                    },
                    {
                        "ConfusionType": "TruePositive",
                        "Class": "B",
                    },
                    {
                        "ConfusionType": "TruePositive",
                        "Class": "B",
                    },
                    {
                        "ConfusionType": "FalsePositive", # x2
                        "Class": "B",
                    },
                    {
                        "ConfusionType": "FalsePositive",
                        "Class": "B",
                    },
                    {
                        "ConfusionType": "FalseNegative", # x1
                        "Class": "B",
                    },
                ]

        sd.add_json_obj(json_obj, "json_obj", set(["A", "B"]), LabelKey="Class")
        # TODO: assert
        pdb.set_trace()
        return