import unittest, os, json, pdb

class TestConvert(unittest.TestCase):
    def __init__(self, *args, **kwargs):
        super(TestConvert, self).__init__(*args, **kwargs)
        
        self.output_dir = os.path.join(
            os.path.dirname(os.path.dirname(__file__)),
            'data',
            'unittest',
            'TestConvert')
        if os.path.exists(self.output_dir) is False: os.makedirs(self.output_dir)
        return   

    def run_convert_intents(self):
        from metrics.code.convert import convert_intents

        # params
        utterances = [
            {
                "query": "add task",
                "gold_intent": "Action_AddAction",
                "pred_intent": "Action_AddAction",
                "pred_intent_score": 0.8
            },
            {
                "query": "add action",
                "gold_intent": "Action_AddAction",
                "pred_intent": "Action_UpdateAction",
                "pred_intent_score": 0.9947372
            },
        ]

        output_dir = os.path.join(
            self.output_dir
        )

        # core func

        convert_intents(
            utterances,
            output_dir)

        return output_dir

    def test_convert_intents_json(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_convert_intents_json
        '''
        output_dir = self.run_convert_intents()

        # expected answer
        res_json = "%s/ClassificationErrorAnalysisReport.json"%output_dir

        with open(res_json, 'r') as fd:
            actual_res = json.load(fd)
            expected_res = [
                {
                    "DocumentId": "0",
                    "ConfusionType": "TruePositive",
                    "Class": "Action_AddAction",
                    "Score": 0.8,
                    "Text": "add task"
                },
                {
                    "DocumentId": "1",
                    "ConfusionType": "FalseNegative",
                    "Class": "Action_AddAction",
                    "Score": 0.9947372,
                    "Text": "add action"
                },
                {
                    "DocumentId": "1",
                    "ConfusionType": "FalsePositive",
                    "Class": "Action_UpdateAction",
                    "Score": 0.9947372,
                    "Text": "add action"
                }
            ]
            assert actual_res==expected_res
        return
    
    def test_convert_intents_tsv(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_convert_intents_tsv
        '''
        output_dir = self.run_convert_intents()

        res_tsv = "%s/ClassificationErrorAnalysisReport.tsv"%output_dir
        with open(res_tsv, 'r') as fd:
            lines = fd.readlines()
            expected_lines = [
                "DocumentId\tConfusionType\tClass\tScore\tText\n",
                "0\tTruePositive\tAction_AddAction\t0.8\tadd task\n",
                "1\tFalseNegative\tAction_AddAction\t0.9947372\tadd action\n",
                "1\tFalsePositive\tAction_UpdateAction\t0.9947372\tadd action\n"]
            assert lines == expected_lines

    def run_convert_entities(self):
        from metrics.code.convert import convert_entities
        utterances = [
            {
                "query": "add task for jason to complete test due Friday",
                "gold_entities": [
                    {
                        "entity": "Action_Assignee",
                        "startPos": 13,
                        "endPos": 17 # inclusive; json -- True positive
                    },
                    {
                        "entity": "Action_TaskContent",
                        "startPos": 22,
                        "endPos": 34 # complete test -- False negative (missing)
                    },
                    {
                        "entity": "Action_DueDate",
                        "startPos": 36,
                        "endPos": 46 # due Friday -- False negative (unmatch)
                    }
                ],
                "pred_entities": [
                    {
                        "entity": "Action_Assignee",
                        "startPos": 13,
                        "endPos": 17, # inclusive; json -- True positive (dup)
                        "score": 0.9
                    },
                    {
                        "entity": "Action_DueDate",
                        "startPos": 40,
                        "endPos": 46, # Friday -- False positive
                        "score": 0.8
                    }
                ],
            }
        ]
        output_dir = os.path.join(
            self.output_dir
        )

        # core func
        convert_entities(
            utterances,
            output_dir)
        return output_dir


    def test_convert_entities_json(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_convert_entities_json
        '''
        output_dir = self.run_convert_entities()
        # expected answer

        res_json = "%s/EntitiesErrorAnalysisReport.json"%output_dir

        with open(res_json, 'r') as fd:
            actual_res = json.load(fd)
            expected_res = [
                {
                    "DocumentId": "0",
                    "ConfusionType": "TruePositive",
                    "Entity": "Action_Assignee",
                    "StartIndex": 13,
                    "Length": 5,
                    "Phrase": "jason",
                    "Context": "add task for jason to complete test due Friday",
                    "Score": 0.9
                },
                {
                    "DocumentId": "0",
                    "ConfusionType": "FalseNegative",
                    "Entity": "Action_TaskContent",
                    "StartIndex": 22,
                    "Length": 13,
                    "Phrase": "complete test",
                    "Context": "add task for jason to complete test due Friday",
                    "Score": -1
                },
                {
                    "DocumentId": "0",
                    "ConfusionType": "FalseNegative",
                    "Entity": "Action_DueDate",
                    "StartIndex": 36,
                    "Length": 11,
                    "Phrase": "due Friday",
                    "Context": "add task for jason to complete test due Friday",
                    "Score": -1
                },
                {
                    "DocumentId": "0",
                    "ConfusionType": "FalsePositive",
                    "Entity": "Action_DueDate",
                    "StartIndex": 40,
                    "Length": 7,
                    "Phrase": "Friday",
                    "Context": "add task for jason to complete test due Friday",
                    "Score": 0.8
                }]
            
            assert actual_res==expected_res

        return

    def test_convert_entities_tsv(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_convert_entities_tsv
        '''
        output_dir = self.run_convert_entities()
        # expected answer

        res_tsv = "%s/EntitiesErrorAnalysisReport.tsv"%output_dir
        with open(res_tsv, 'r') as fd:
            lines = fd.readlines()
            expected_lines = [
                "DocumentId\tConfusionType\tEntity\tScore\tStartIndex\tLength\tPhrase\tContext\n",
                "0\tTruePositive\tAction_Assignee\t0.9\t13\t5\tjason\tadd task for jason to complete test due Friday\n",
                "0\tFalseNegative\tAction_TaskContent\t-1\t22\t13\tcomplete test\tadd task for jason to complete test due Friday\n",
                "0\tFalseNegative\tAction_DueDate\t-1\t36\t11\tdue Friday\tadd task for jason to complete test due Friday\n",
                "0\tFalsePositive\tAction_DueDate\t0.8\t40\t7\tFriday\tadd task for jason to complete test due Friday\n"]
            assert lines == expected_lines


    def test_process_collect_stat(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_process_collect_stat
        '''
        from metrics.code.main import process_collect_stat
        from metrics.code.config import Options

        res_dir_list = [
            self.output_dir
        ]

        intent_json = os.path.join(self.output_dir, "ClassificationErrorAnalysisReport.json")
        entity_json = os.path.join(self.output_dir, "EntitiesErrorAnalysisReport.json")
        if os.path.exists(intent_json) is False or os.path.exists(entity_json) is False:
            print("test_process_collect_stat: %s or %s n/a"%(intent_json, entity_json))
            return

        res_dir_description_list = [
            "TestConvert"
        ]

        out_dir = self.output_dir

        options = Options()
        options.dic["collect_stat"] = {
                "enable": True,
                "entity": {
                    "Files": [
                        "EntitiesErrorAnalysisReport.json"
                    ],
                    "LabelSet": set([
                        "Action_Assignee",
                        "Action_DueDate",
                        "Action_TaskContent"              
                    ]),
                    "LabelKey": "Entity",
                    "PrintLabelOrder": [
                        "Action_Assignee",
                        "Action_DueDate",
                        "Action_TaskContent"
                    ]
                },
                "intent": {
                    "Files": [
                        "ClassificationErrorAnalysisReport.json"
                    ],
                    "LabelSet": set([
                        'Action_AddAction',
                        'Action_UpdateAction'                    
                    ]),
                    "LabelKey": "Class",
                    "PrintLabelOrder": [
                        'Action_AddAction',
                        'Action_UpdateAction'
                    ]
                }
            }
        
        process_collect_stat(
            res_dir_list,
            res_dir_description_list,
            out_dir,
            options
        )

        return

    def test_label_entities(self):
        '''
        usage: python -m unittest metrics.code.convert_test.TestConvert.test_label_entities
        '''
        from metrics.code.convert import label_entities

        items = [
            {
                "utterance": "add a task",
                "sorted_entities": [],
                "labeled_utterance": "add a task"
            },
            {
                "utterance": "add task to get demo script demo ready by Friday, assigned to Marieke",
                "sorted_entities": \
                  [
                    {
                        "Label": "Action_TaskContent",
                        "Start": 12,
                        "Length": 26
                    },
                    {
                        "Label": "Action_DueDate",
                        "Start": 39,
                        "Length": 9
                    },
                    {
                        "Label": "Action_Assignee",
                        "Start": 62,
                        "Length": 7
                    }
                  ],
                "labeled_utterance": "add task to {@Action_TaskContent=get demo script demo ready} {@Action_DueDate=by Friday}, assigned to {@Action_Assignee=Marieke}"
            }
        ]

        for item in items:
            labeled_utterance = label_entities(
                item["utterance"],
                item["sorted_entities"]
            )
            print(labeled_utterance)
            assert labeled_utterance==item["labeled_utterance"]

        return