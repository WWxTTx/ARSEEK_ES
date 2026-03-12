using System.Collections.Generic;
using System.Linq;
using UnityFramework.Runtime;

public partial class ExamCoursePanel : OPLCoursePanel/*HoverHintPanel*/
{
    #region 模拟练习
    public class Score
    {
        public int value;
        public int wrong;
        public int correct;
        public bool complete;
        public virtual void SetScore(int index, bool value)
        {

        }
        public virtual void UpdateScore()
        {

        }
        public virtual void Submit()
        {

        }
    }

    public class SmallSceneScoreDetail
    {
        public int value;
        public bool? submit;
        public SmallSceneScoreDetail(int value)
        {
            this.value = value;
            submit = null;
        }
    }

    public class SmallSceneScore : Score
    {
        public List<SmallSceneScoreDetail> detail;
        public override void SetScore(int index, bool value)
        {
            base.SetScore(index, value);
            if (index < detail.Count)
            {
                if (detail[index].submit == null)
                {
                    detail[index].submit = value;
                }
                else
                {
                    Log.Debug($"正在尝试重复覆盖成绩{index}");
                }
            }
            else
            {
                Log.Error($"超界{index} {detail.Count}");
            }
        }
        public override void UpdateScore()
        {
            value = 0;
            wrong = 0;
            correct = 0;
            complete = true;

            foreach (var detail in detail.GroupBy(value => value.submit))
            {
                if (detail.Key == true)
                {
                    value = detail.Sum(value => value.value);
                    correct = detail.Count();
                }
                else if (detail.Key == false)
                {
                    wrong = detail.Count();
                }
                else
                {
                    complete = false;
                }
            }
        }
        public override void Submit()
        {
            base.Submit();
            foreach (var data in detail)
            {
                if (data.submit == null)
                {
                    data.submit = false;
                }
            }
            UpdateScore();
        }
    }
    #endregion
}