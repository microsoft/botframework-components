import datetime
import pdb 

class StatData:
    def __init__(self):
        '''
        {
            'label': # res_dir (e.g. train / test) + task_type (e.g. intent / entity) + file (e.g. thre)
            {
                "breakdown": {
                    entity_label: {
                        fp: cnt_fp,
                        fn: cnt_fn,
                        tp: cnt_tp,
                        n_examples: = fn + tp
                        precision: = tp / (tp + fp)
                        recall: = tp / (tp + fn)
                        f1: = 2 prec * recall / (prec + recall)
                    }
                },
            }
        }
        '''
        self.data = {}
        return
    
    def get_table_str(self, LabelList):
        """
        get a str of table view
        """

        res_str = "Timestamp,LabelName,F1,Precision,Recall,NumLabels,Tp,Fp,Fn\n"

        for label in LabelList:
            dic = self.data.get("breakdown", {}).get(label, {})
            if dic=={}: continue 

            res_str += datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S') + "," +  label + "," +\
                        str(dic["f1"]) + "," +\
                        str(dic["precision"]) + "," +\
                        str(dic["recall"]) + "," +\
                        str(dic["n_examples"]) + "," +\
                        str(dic["tp"]) + "," +\
                        str(dic["fp"]) + "," +\
                        str(dic["fn"]) + "\n"

        return res_str

    def add_json_obj(self, json_obj, LabelSet, LabelKey):
        '''
        Input:
          json_obj: an object obtained from ClassificationReport.json or EntitiesReport.json
            for intent: e.g.
                [
                    {
                        "DocumentId": "0",
                        "ConfusionType": "FalseNegative",
                        "Class": "ACTION_ADDACTION_LX",
                        "Score": null,
                        "Text": "Create"
                    },
                ]         
            for entity: e.g.
                [
                    {
                        "DocumentId": "1",
                        "ConfusionType": "TruePositive",
                        "Entity": "ACTION_PRIORITY",
                        "StartIndex": 9,
                        "Length": 12,
                        "Phrase": "low priority",
                        "Context": "create a low priority task to revisit the logging"
                    },
                ]
          json_obj_label: a label describing the .json
            e.g. train_as_test or test_as_test,
                 before_lu_modi or after_lu_modi
          LabelSet: a set of intent / entity from json_obj
            e.g. ACTION_ADDACTION_LX etc
          LabelKey: "Class" for intent and "Entity" for entity
        '''
        labels = sorted(list(LabelSet))
        res = {
            "breakdown": {}}
        for label in labels:
            res["breakdown"][label] = {"fp": 0, "fn": 0, "tp": 0, "n_examples": 0, "precision": 0, "recall": 0, "f1": 0}

        for dic in json_obj:
            label = dic[LabelKey]
            ct = dic["ConfusionType"]

            if label not in LabelSet: continue 

            if ct=="TruePositive":
                res["breakdown"][label]["tp"] += 1

            elif ct=="FalsePositive":
                res["breakdown"][label]["fp"] += 1
            
            elif ct=="FalseNegative":
                res["breakdown"][label]["fn"] += 1
        
            else:
                print("unknown ct=%s"%ct)
                pdb.set_trace()
        
        macro_prec = 0
        macro_rec = 0
        macro_cnt = 0

        micro_fp = 0
        micro_fn = 0
        micro_tp = 0

        for label in labels:
            fp = res["breakdown"][label]["fp"]
            fn = res["breakdown"][label]["fn"]
            tp = res["breakdown"][label]["tp"]

            res["breakdown"][label]["n_examples"] = tp + fn 
            prec = float_div(tp, tp + fp)
            res["breakdown"][label]["precision"] = prec
            rec = float_div(tp, tp + fn)
            res["breakdown"][label]["recall"] = rec
            res["breakdown"][label]["f1"] = float_div(2 * prec * rec, prec + rec)

            micro_fp += fp 
            micro_fn += fn 
            micro_tp += tp 

            macro_prec += prec 
            macro_rec += rec 
            macro_cnt += 1

        self.data = res 

        return

def float_div(a, b):
    if b==0:
        return 0.0
    else:
        return float(a) / b
