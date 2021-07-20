import matplotlib.pyplot as plt 
import numpy as np
import pdb
import seaborn  as sns

class BarData:
    def __init__(self):
        '''
        {
            'Groups': [grp1_label, grp2_label, ..., grpN_label]

            'Precision'/'Recall'/'F1': {
                'Label': [grp1_val, grp2_val, ..., grpN_val]
            }
        }
        '''
        self.data = {}
        return
    
    def add_ClassifierMetrics(self, json_obj, json_obj_label, ComparisonType_Thre_Set, NameSet):
        '''
        Input:
          json_obj: an object obtained from ClassifiersMetrics.json
          json_obj_label: a label describing the ClassifiersMetrics.json
            e.g. train_as_test or test_as_test,
                 before_lu_modi or after_lu_modi
          ComparisonType_Thre_Set: a set of (ComparisonType, Threshold) from json_obj
          NameSet: a set of Names from json_obj
            e.g. ACTION_ADDACTION_LX etc
        
        Note:
          - the json_obj_label + ComparisonType + Threshold will serve as curve label
          - the NameSet will serve as Groups
        '''
        SortedNameList = sorted(list(NameSet))
        if 'Groups' in self.data:
            assert self.data['Groups'] == SortedNameList
        else:
            self.data['Groups'] = SortedNameList
        
        Name2IdxDic = {}
        for i in range(len(SortedNameList)):
            Name2IdxDic[SortedNameList[i]] = i

        for dic in json_obj:
            try:
                if (dic['ComparisonType'], dic['Threshold']) not in ComparisonType_Thre_Set:
                    continue
            except:
                pdb.set_trace()

            curve_label = 'res=%s cmp=%s thre=%s'%(json_obj_label, dic['ComparisonType'], dic['Threshold'])
            panels = ['Precision', 'Recall', 'F1']
            for panel in panels:
                self.data.setdefault(panel, {}).setdefault(curve_label, [0]*len(SortedNameList))

            classifiers_metrics = dic['ClassifiersMetrics']
            for metrics in classifiers_metrics:
                if metrics['Name'] not in NameSet: continue

                for panel in panels:
                    score = metrics['Scores'][panel]
                    idx = Name2IdxDic[metrics['Name']]
                    self.data[panel][curve_label][idx] = score

        return

    def add_ExtractorsMetrics(self, json_obj, json_obj_label, MetricsType_Set, NameSet):
        '''
        Input:
          json_obj: an object obtained from ClassifiersMetrics.json
          json_obj_label: a label describing the ClassifiersMetrics.json
            e.g. train_as_test or test_as_test,
                 before_lu_modi or after_lu_modi
          MetricsType_Set: a set of MetricsType from json_obj
            e.g. ExtractorsNonStrictMetrics
          NameSet: a set of Names from json_obj
            e.g. ACTION_ADDACTION_LX etc
        
        Note:
          - the json_obj_label + MetricsType will serve as curve label
          - the NameSet will serve as Groups
        '''
        SortedNameList = sorted(list(NameSet))
        if 'Groups' in self.data:
            assert self.data['Groups'] == SortedNameList
        else:
            self.data['Groups'] = SortedNameList
        
        Name2IdxDic = {}
        for i in range(len(SortedNameList)):
            Name2IdxDic[SortedNameList[i]] = i

        for metric_type in json_obj.keys():
            if metric_type not in MetricsType_Set: continue

            curve_label = 'res=%s tp=%s'%(
                json_obj_label, metric_type)
            panels = ['Precision', 'Recall', 'F1']
            for panel in panels:
                self.data.setdefault(panel, {}).setdefault(curve_label, [0]*len(SortedNameList))

            metrics_list = json_obj[metric_type]
            for metrics in metrics_list:
                if metrics['Name'] not in NameSet: continue

                for panel in panels:
                    score = metrics['Scores'][panel]
                    idx = Name2IdxDic[metrics['Name']]
                    self.data[panel][curve_label][idx] = score

        return

class RocData:
    def __init__(self):
        '''
        self.data = {
            [intent/entity]: {
                'curve_label': {
                    'x': [x_val1, x_val2, ...],
                    'y': [y_val1, y_val2, ...],
                }
            }
        }
        '''
        self.data = {}
        return 
    
    def add_ClassifierMetrics(
        self,
        json_obj,
        json_obj_label,
        NameSet,
        x_axis="Precision",
        y_axis="Recall"):
        '''
        Input:
          json_obj: an object obtained from ClassifiersMetrics.json
          json_obj_label: a label describing the ClassifiersMetrics.json
            e.g. train_as_test or test_as_test,
                 before_lu_modi or after_lu_modi
          NameSet: a set of (intent) names serving as panels
          x_axis: by default is Precision for ROC curve
          y_axis: by default is Recall for ROC curve

        Note:
          - for the json_obj, we'll create two curves.
            First one is a curve with changing thresholds (label=json_obj_label + Threshold)
            Second one is a curve with TopScoring (label=json_obj_label + TopScore)\
        '''
        SortedNameList = sorted(list(NameSet))
        for name in SortedNameList:
            self.data.setdefault(name, {})
        
        curve_label_set = set()
        for dic in json_obj:
            tp = dic['ComparisonType']
            
            if tp=="Threshold" or tp=="All":
                thre = dic['Threshold'] if dic['Threshold'] is not None else 0
                curve_label = '%s Threshold'%(json_obj_label)

            elif tp=="TopScoring":
                #pdb.set_trace()
                thre = -1
                curve_label = '%s TopScoring'%(json_obj_label)
            else:
                print('unknown ComparisonType: %s'%tp)
                raise Exception 

            for metrics in dic['ClassifiersMetrics']:
                panel = metrics['Name']
                if panel not in NameSet: continue 

                self.data[panel].setdefault(curve_label, {'x':[], 'y':[]})
                self.data[panel][curve_label]['x'].append((thre, metrics['Scores'][x_axis])) # a list of (thre, x_val)
                self.data[panel][curve_label]['y'].append((thre, metrics['Scores'][y_axis])) # a list of (thre, y_val)
                
                curve_label_set.add(curve_label)
        
        for panel in self.data.keys():
            for curve_label in curve_label_set: # some curve_label are processed in prev RocData.add_ClassifierMetrics
                if self.data[panel]=={} or curve_label not in self.data[panel]:
                    continue 

                self.data[panel][curve_label]['x'] = sorted(self.data[panel][curve_label]['x'], key=lambda xx: xx[0]) # sort by thre
                self.data[panel][curve_label]['x'] = [itm[1] for itm in self.data[panel][curve_label]['x']]

                self.data[panel][curve_label]['y'] = sorted(self.data[panel][curve_label]['y'], key=lambda yy: yy[0]) # sort by thre
                self.data[panel][curve_label]['y'] = [itm[1] for itm in self.data[panel][curve_label]['y']]

        return

class ConfusionMatrixData:
    def __init__(self):
        '''
        self.data = {
            <res+intent+thre or res+entity>: {
                row_labels: n_row(matrix),
                col_labels: n_col(matrix),
                matrix: 2-d array,
            }
        }
        '''
        self.data = {}
        return
    
    def add_ClassifierErrorAnalysisReport(
        self,
        description,
        annot_data,
        res_data):
        '''
        Input:
          description: serve as panel name
          annot_data: a list of {
              "text":..., 
              "intent":...,
          }
          res_data: a list of {
              "DocumentId":
              "ConfusionType":
              "Class": <predicted_intent>,
              "Score": (could be null),
              "Text":
          }     

        TODO:
        - dump specific samples for particular confusion type (e.g.
          expected=add_action predicted=None)     
        '''

        '''
        1. collect ref intents
        '''
        ref_intents = set([])
        ref_dic = {}  # key: doc id str, val: {"text", "intent"}
        for i in range(len(annot_data)):
            ref_intents.add(annot_data[i]["intent"])
            ref_dic[str(i)] = {
                "text": annot_data[i]["text"],
                "intent": annot_data[i]["intent"]
            }
        
        '''
        2. collect res intents
        '''
        res_intents = set([])
        for i in range(len(res_data)):
            res_intents.add(res_data[i]["Class"])
        # res_intents.add("None")

        '''
        3. row_labels and col_labels
        '''
        uncovered_ref_intents = set(
            [r for r in ref_intents if r not in res_intents])
        assert len(uncovered_ref_intents)==0

        row_labels = sorted(list(ref_intents))
        row_label2idx = {}
        for i in range(len(row_labels)):
            row_label2idx[row_labels[i]] = i 
        
        col_labels = sorted(list(res_intents))
        col_label2idx = {}
        for i in range(len(col_labels)):
            col_label2idx[col_labels[i]] = i 
        if "None" not in col_label2idx:
            col_labels.append("None")
            col_label2idx["None"] = len(col_labels)-1
        
        '''
        4. matrix
        '''
        matrix = np.zeros(
            (len(row_labels), len(col_labels)),
            dtype=int)
        
        for res_item in res_data:
            doc_id = res_item["DocumentId"]
            score = res_item["Score"]
            res_intent = res_item["Class"]
            confusion_tp = res_item["ConfusionType"]

            expected_text = ref_dic[doc_id]["text"]
            expected_intent = ref_dic[doc_id]["intent"]

            '''
            sanity check
            '''
            assert expected_text == res_item["Text"]

            row_idx = row_label2idx[expected_intent]

            if score is None:
                assert res_intent == expected_intent and confusion_tp == "FalseNegative"
                col_idx = col_label2idx["None"]
            else:
                if res_intent == expected_intent:
                    assert confusion_tp == "TruePositive"
                else:
                    assert confusion_tp == "FalsePositive"
                col_idx = col_label2idx[res_intent]
            
            matrix[row_idx][col_idx] += 1

        '''
        5. add panel
        '''
        assert description not in self.data 
        self.data[description] = {
            "row_labels": row_labels,
            "col_labels": col_labels,
            "matrix": matrix
        }

        return

def plot_bar_data(bd: BarData, dst_path: str = ""):   
    '''
    plot for the BarData bd, save to dst_path if valid
    '''
    panels = ['Precision', 'Recall', 'F1']
    groups = bd.data['Groups']
    fig_W, fig_H = [9, 9]

    fig = plt.figure(figsize=[fig_W, fig_H])
    for panel_idx in range(len(panels)):
        panel = panels[panel_idx]
        ax = fig.add_subplot(len(panels), 1, panel_idx+1)

        label_list = sorted(list(bd.data[panel].keys()))

        x = np.arange(len(groups))
        width = min(fig_W / len(groups) / len(label_list), 0.08)

        rects_list = []
        shift = width * len(label_list) / 2 - width / 2
        for rects_idx in range(len(label_list)):
            rects_list.append(
                ax.bar(
                    x + rects_idx * width - shift,
                    bd.data[panel][label_list[rects_idx]],
                    width,
                    label = label_list[rects_idx]
                )
            )
        
        ax.set_xticks(x)
        ax.set_xticklabels(
            groups,
            fontsize='xx-small',
            rotation=45)
        if (panel_idx < len(panels)-1):
            plt.setp(ax.get_xticklabels(), visible=False)

        ax.set_ylabel(panel)

        ax.legend(
            #loc='best',
            bbox_to_anchor=(1, 1),
            fontsize='xx-small')

    fig.tight_layout()
    if dst_path == "":
        plt.show()
    else:
        plt.savefig(dst_path)
        print('%s drawn'%dst_path)
    return

def plot_roc_data(rd: RocData, dst_path: str = "", x_axis = "", y_axis = ""):
    '''
    plot for the RocData rd, save to dst_path if valid
    '''
    panels = sorted(list(rd.data.keys()))

    fig_W, fig_H = [9, 9]

    fig = plt.figure(figsize=[fig_W, fig_H])
    for panel_idx in range(len(panels)):
        panel = panels[panel_idx]
        ax = fig.add_subplot((len(panels)+1)/2, 2, panel_idx+1)

        label_list = sorted(list(rd.data[panel].keys()))
        for label in label_list:
            x = rd.data[panel][label]['x']
            y = rd.data[panel][label]['y']
            ax.plot(
                x, y, 
                linestyle='--',
                marker='.',
                label=label)
            ax.legend(
                loc='best',
                fontsize='xx-small'
            )

        if x_axis != "": ax.set_xlabel(x_axis)
        if y_axis != "": ax.set_ylabel(y_axis)
        ax.set_title(panel)
    
    fig.tight_layout()
    if dst_path == "":
        plt.show()
    else:
        plt.savefig(dst_path)
        print('%s drawn'%dst_path)
    return

def plot_confusion_matrix_data(cd: ConfusionMatrixData, dst_path: str = ""):
    '''
    plot for the ConfusionMatrixData cd, save to dst_path if valid
    '''
    panels = sorted(list(cd.data.keys()))
    fig_W, fig_H = [9, 9* len(panels)]

    fig = plt.figure(figsize=[fig_W, fig_H])
    for panel_idx in range(len(panels)):
        panel = panels[panel_idx]

        row_labels = cd.data[panel]['row_labels']
        col_labels = cd.data[panel]['col_labels']
        matrix = cd.data[panel]['matrix']

        ax = fig.add_subplot((len(panels)+1), 1, panel_idx+1)

        sns.heatmap(
            matrix,
            ax=ax,
            annot=True, fmt="d", annot_kws={'fontsize': 'xx-small'},
            xticklabels=col_labels,
            yticklabels=row_labels,
            linewidths=0.01)

        ax.set_ylabel('expected')
        ax.set_xlabel('predicted')
        ax.set_title(panel)
    
    fig.tight_layout()
    if dst_path == "":
        plt.show()
    else:
        plt.savefig(dst_path)
        print('%s drawn'%dst_path)
    return