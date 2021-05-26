'''
convert predictions.json into e.g.
- ClassificationErrorAnalysisReport.json
- EntitiesErrorAnalysisReport.json
for downstream metric analysis
'''

import os, json, pdb, sys, csv

def write_tsv(file_name, columns, lines):
    with open(file_name, "w", newline='') as fd:
        writer = csv.writer(fd, delimiter='\t')
        writer.writerow(columns)
        for example in lines:
            writer.writerow([example[col] for col in columns])


def convert_intents(
    utterances,
    output_dir,
    target_intents=None
    ):
    '''
    Input:
      utterances: a list of
        {
            "query": <utterance>,
            "gold_intent": <gold_intent>,
            "pred_intent": <pred_intent>,
            "gold_entities": [
                {
                    "entity": <entity>,
                    "startPos": <startPos>,
                    "endPos": <endPos>
                },
                ...
            ],
            "pred_entities": [
                {
                    "entity": <entity>,
                    "startPos": <startPos>,
                    "endPos": <endPos>,
                    "score": <score>
                },
                ...
            ]
        }
      target_intents: set of target intents, default None (all)
    Output:
      <output_dir>/ClassificationErrorAnalysisReport.json
    '''
    res = []
    doc_id = -1

    for utterance_dic in utterances:
        doc_id += 1

        gold_intent = utterance_dic.get("gold_intent", None)
        pred_intent = utterance_dic.get("pred_intent", None)

        if gold_intent is None or pred_intent is None:
            print(utterance_dic)
            raise Exception("gold_intent or pred_intent not found")

        if target_intents is not None and gold_intent not in target_intents:
            print("skip none target intent. utterance=%s"%utterance_dic.get("query", "n/a"))
            continue 

        if gold_intent==pred_intent:
            res.append(
                {
                    "DocumentId": str(doc_id),
                    "ConfusionType": "TruePositive",
                    "Class": gold_intent,
                    "Score": utterance_dic.get("pred_intent_score", -1),
                    "Text": utterance_dic.get("query", "n/a")
                }
            )
        else:
            res.append(
                {
                    "DocumentId": str(doc_id),
                    "ConfusionType": "FalseNegative",
                    "Class": gold_intent,
                    "Score": utterance_dic.get("pred_intent_score", -1),
                    "Text": utterance_dic.get("query", "n/a")
                }
            )     
            res.append(
                {
                    "DocumentId": str(doc_id),
                    "ConfusionType": "FalsePositive",
                    "Class": pred_intent,
                    "Score": utterance_dic.get("pred_intent_score", -1),
                    "Text": utterance_dic.get("query", "n/a")
                }
            )   

    if os.path.exists(output_dir) is False:
        os.mkdir(output_dir) 
    res_path = os.path.join(output_dir, "ClassificationErrorAnalysisReport.json")
    fd = open(res_path, "w")
    json.dump(res, fd, indent=1)
    fd.close()
    print("%s written"%res_path)

    # Write as tsv
    tsv_path = os.path.join(output_dir, "ClassificationErrorAnalysisReport.tsv")
    write_tsv(tsv_path, ['DocumentId', 'ConfusionType', 'Class', 'Score', 'Text'], res)
    print("%s written"%tsv_path)

    return

def convert_entities(
    utterances,
    output_dir,
    target_entities=None
    ):
    '''
    Input:
      utterances: a list of
        {
            "query": <utterance>,
            "gold_intent": <gold_intent>,
            "pred_intent": <pred_intent>,
            "gold_entities": [
                {
                    "entity": <entity>,
                    "startPos": <startPos>,
                    "endPos": <endPos>
                },
                ...
            ],
            "pred_entities": [
                {
                    "entity": <entity>,
                    "startPos": <startPos>,
                    "endPos": <endPos>,
                    "score": <score>
                },
                ...
            ]
        }
      target_entities: set of target entities, default None (all)
    Output:
      <output_dir>/EntitiesErrorAnalysisReport.json
    '''
    res = []
    doc_id = -1

    for utterance_dic in utterances:
        doc_id += 1

        gold_entities = utterance_dic.get("gold_entities", [])
        pred_entities = utterance_dic.get("pred_entities", [])
        if gold_entities==[] and pred_entities==[]: continue 

        utterance = utterance_dic["query"]

        # key is (startPos, endPos, entity_label), val is matched (startPos, endPos, entity_label) or None
        gold_tup2match = {}
        pred_tup2match = {}
        pred_tup_score = {}

        for gold_entity in gold_entities:
            ent = gold_entity["entity"]
            startPos = gold_entity["startPos"]
            endPos = gold_entity["endPos"]
            gold_tup2match[(startPos, endPos, ent)] = None 
        
        for pred_entity in pred_entities:
            ent = pred_entity["entity"]
            startPos = pred_entity["startPos"]
            endPos = pred_entity["endPos"]
            tup = (startPos, endPos, ent)
            pred_tup_score[tup] = pred_entity.get("score", -1)

            if tup in gold_tup2match:
                gold_tup2match[tup] = tup 
                pred_tup2match[tup] = tup 
            else:
                pred_tup2match[tup] = None
        
        for tup in gold_tup2match.keys():
            StartIndex = tup[0]
            Length = tup[1] - tup[0] + 1 # endPos inclusive
            Entity = tup[2]
            Phrase = utterance[StartIndex:StartIndex+Length]
            Context = utterance # we keep whole utterance instead of a window of utterance
            Score = -1

            if gold_tup2match[tup] is None:
                ConfusionType = "FalseNegative"
            else:
                ConfusionType = "TruePositive"
                Score = pred_tup_score[tup]
            
            res.append(
                {
                    "DocumentId": str(doc_id),
                    "ConfusionType": ConfusionType,
                    "Entity": Entity,
                    "StartIndex": StartIndex,
                    "Length": Length,
                    "Phrase": Phrase,
                    "Context": Context,
                    "Score": Score
                }
            )
        
        for tup in pred_tup2match.keys():
            StartIndex = tup[0]
            Length = tup[1] - tup[0] + 1 # endPos inclusive
            Entity = tup[2]
            Phrase = utterance[StartIndex:StartIndex+Length]
            Context = utterance # we keep whole utterance instead of a window of utterance
            Score = pred_tup_score[tup]

            if pred_tup2match[tup] is None:
                ConfusionType = "FalsePositive"
            else:
                continue # entity prediction result (TruePositive) already handled
            
            res.append(
                {
                    "DocumentId": str(doc_id),
                    "ConfusionType": ConfusionType,
                    "Entity": Entity,
                    "StartIndex": StartIndex,
                    "Length": Length,
                    "Phrase": Phrase,
                    "Context": Context,
                    "Score": Score
                }
            )
        
    # output
    if os.path.exists(output_dir) is False:
        os.mkdir(output_dir) 
    res_path = os.path.join(output_dir, "EntitiesErrorAnalysisReport.json")
    fd = open(res_path, "w")
    json.dump(res, fd, indent=1)
    fd.close()
    print("%s written"%res_path)

    tsv_path = os.path.join(output_dir, "EntitiesErrorAnalysisReport.tsv")
    write_tsv(tsv_path, ['DocumentId', 'ConfusionType', 'Entity', 'Score', 'StartIndex', 'Length', 'Phrase', 'Context'], res)
    print("%s written"%tsv_path)

    return

def convert(
    predictions_json,
    output_dir,
    target_intents=None,
    target_entities=None,
    ):
    '''
    Input:
      predictions_json: a json file containing dic of structure:
        {
            "intents": ["Action_AddAction", ...],
            "entities": ["Action_Assignee", ...],
            "utterances": [
                {
                    "query": <utterance>,
                    "gold_intent": <gold_intent>,
                    "pred_intent": <pred_intent>,
                    "gold_entities": [
                        {
                            "entity": <entity>,
                            "startPos": <startPos>,
                            "endPos": <endPos>
                        },
                        ...
                    ],
                    "pred_entities": [
                        {
                            "entity": <entity>,
                            "startPos": <startPos>,
                            "endPos": <endPos>
                        },
                        ...
                    ]
                },
                ...
            ]
        }
      target_intents: set of target intents, default None (all)
      target_entities: set of target entities, default None (all)
    Output:
      <output_dir>/ClassificationErrorAnalysisReport.json
      <output_dir>/EntitiesErrorAnalysisReport.json
    
    TODO:
    - use subset of target_intents or target_entities
      this may not be necessary as downstream analysis (based on ClassificationErrorAnalysisReport.json or EntitiesErrorAnalysisReport.json)
      will specify additional target_intents or target_entities
    '''
    fd = open(predictions_json, "r")
    predictions = json.load(fd)
    fd.close()

    convert_intents(predictions.get("utterances", []), output_dir, target_intents)

    convert_entities(predictions.get("utterances", []), output_dir, target_entities)    

    return

def convert_LuisNLP_dataset_json_to_lu_style(
    input_json,
    output_lu):
    '''
    input_json:
      a json file containing LuisNLP dataset items, e.g. per item looks like:
        {
            "Text": "add task to get demo script demo ready by Friday, assigned to Marieke",
            "Classes": [
                "Action_AddAction"
            ],
            "Entities": [
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
            ]
        },
    
    output_lu:
      converted from input_json to lu style text. e.g.
      ## Action_AddAction
      - add task to {@Action_TaskContent=get demo script demo ready} {@Action_DueDate=by Friday},
        assigned to {@Action_Assignee=Marieke}

    Note:
    - we still need to paste the contents from output_lu to a more complete lu file for test?

    TODO: hierarchy entities
    '''
    if os.path.exists(input_json) is False:
        raise Exception("%s not exist"%input_json)

    out_dir = os.path.dirname(output_lu)
    if os.path.exists(out_dir) is False:
        os.makedirs(out_dir)  

    f = open(input_json, 'r')
    items = json.load(f)
    f.close()

    res = {} # key: intent val: list of entity-labeled utterances
    for itm in items:
        utterance = itm["Text"]
        intent = itm["Classes"][0]
        sorted_entities = sorted(itm["Entities"], key=lambda x: x["Start"])

        new_utterance = label_entities(utterance, sorted_entities)

        res.setdefault(intent, []).append(new_utterance)
    
    with open(output_lu, 'w') as fo:
        intents = sorted(res.keys())

        for intent in intents:
            fo.write("## %s\n"%intent)

            for labeled_utterance in res[intent]:
                fo.write("- %s\n"%labeled_utterance)

            fo.write("\n")
        
        print("%s written"%output_lu)

    return

def label_entities(utterance, sorted_entities):
    '''
    Input:
      utterance: e.g. add task to get demo script demo ready by Friday, assigned to Marieke
      sorted_entities: e.g. 
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
        ]
    Output:
      labeled_utterance: e.g. add task to {@Action_TaskContent=get demo script demo ready} {@Action_DueDate=by Friday},
        assigned to {@Action_Assignee=Marieke}
    '''
    labeled_utterance = ""
    pos = 0 # [0, pos) has been processed

    for ent in sorted_entities:
        start = ent["Start"]
        end = start + ent["Length"] # exclusive
        
        # [start, end) should be within utterance[pos, len(utterance))
        if start < pos or end > len(utterance): continue # invalid, skip

        chunk1 = utterance[pos: start]
        chunk2 = utterance[start: end]
        chunk2 = "{@%s=%s}"%(ent["Label"], chunk2)

        labeled_utterance += chunk1 + chunk2

        pos = end
    
    labeled_utterance += utterance[pos:]
    return labeled_utterance

'''
Usage:
  python metrics/code/convert.py LuisNlpJson2lu -i input_json -o output_lu
'''
def main(arguments):
    print(arguments)

    mode = arguments[1]
    args = arguments[2:]

    if mode=='LuisNlpJson2lu':
        input_json = args[args.index('-i')+1]
        output_lu = args[args.index('-o')+1]
        convert_LuisNLP_dataset_json_to_lu_style(input_json, output_lu)
    else:
        raise Exception("unknown mode: %s"%mode)

    return

if __name__ == '__main__':
    args = sys.argv
    main(args)