using System.Collections.Generic;

namespace App.Shared.Common
{
    /// <summary>
    /// State基底 / 不要な関数は実装しなくて良いようにinterfaceではなくabstract
    /// </summary>
    public abstract class IState
    {
        /// <summary>StateController</summary>
        public StateController Ctrl { get; private set; }

        /// <summary>今動作中か</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// State追加時の初期化 / falseを呼ぶと次のStateへ
        /// </summary>
        /// <returns></returns>
        public virtual void Enter() { }

        /// <summary>
        /// State内のメインループ / falseを呼ぶと次のStateへ
        /// </summary>
        /// <returns></returns>
        public virtual bool Update(float dt) { return false; }

        /// <summary>
        /// State内の描画関数
        /// </summary>
        /// <param name="_dt"></param>
        public virtual void Draw(float dt) { }

        /// <summary>
        /// State終了時処理 / falseを呼ぶと次のStateへ
        /// </summary>
        /// <returns></returns>
        public virtual IState Exit() { return null; }

        /// <summary>
        /// 一時中断から復帰するとき
        /// </summary>
        /// <param name="_prevState"></param>
        public virtual void CalledOnResume(IState prevState) { }

        /// <summary>
        /// 他のLoopに遷移して一時停止するとき
        /// </summary>
        /// <param name="_nextState"></param>
        public virtual void CalledOnPause(IState nextState) { }

        /// <summary>
        /// StateController設定(internal)
        /// </summary>
        /// <param name="_ctrl"></param>
        internal void SetController(StateController ctrl)
        {
            Ctrl = ctrl;
        }

        /// <summary>
        /// 今生きてるか
        /// </summary>
        /// <param name="_isActive"></param>
        internal void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }

    /// <summary>
    /// State操作
    /// </summary>
    public class StateController
    {
        /// <summary>予約できるMax数</summary>
        const int RESERVED_MAX = 20;

        /// <summary>state stack</summary>
        private Stack<IState> stateStack = new Stack<IState>();

        /// <summary>State追加予約</summary>
        private Queue<IState> reservedStateQueue = new Queue<IState>(RESERVED_MAX);

        /// <summary>
        /// 実行中
        /// </summary>
        public IState Run(float dt)
        {
            while (reservedStateQueue.Count > 0)
            {
                addState(reservedStateQueue.Dequeue());
            }

            if (stateStack.Count <= 0)
            {
                return null;
            }

            IState st = stateStack.Peek();
            if (st.Update(dt))
            {   // Update継続中
                st.Draw(dt);
                return st;
            }

            IState prevState = st;
            IState nextState = st.Exit();
            stateStack.Pop();

            if (nextState != null)
            {   // 次のStateがあるなら追加しとく
                ReserveAddState(nextState);
            }
            else if (stateStack.Count > 0)
            {   // なければ前のStack戻る
                st = stateStack.Peek();
                st.SetActive(true);
                st.CalledOnResume(prevState);
            }

            return nextState;
        }

        /// <summary>
        /// 現在のState取得
        /// </summary>
        /// <returns></returns>
        public IState GetCurrentState()
        {
            return stateStack.Count > 0 ? stateStack.Peek() : null;
        }

        /// <summary>
        /// State追加予約
        /// </summary>
        /// <param name="state"></param>
        public void ReserveAddState(IState state)
        {
            reservedStateQueue.Enqueue(state);
        }

        /// <summary>
        /// State追加
        /// </summary>
        /// <param name="state"></param>
        private void addState(IState state)
        {
            // Ctrlセット
            state.SetController(this);

            // 次のStateへ行く前にPauseしとく
            if (stateStack.Count > 0)
            {
                var prev = stateStack.Peek();
                prev.CalledOnPause(state);
                prev.SetActive(false);
            }

            stateStack.Push(state);
            state.SetActive(true);
            state.Enter();
        }
    }
}