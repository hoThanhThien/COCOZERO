from collections import OrderedDict
from datetime import datetime, timezone
from typing import Any


class SessionContextStore:
    """Bounded in-memory session context manager (no database schema dependency)."""

    def __init__(self, max_sessions: int = 200, max_turns_per_session: int = 12) -> None:
        self._sessions: OrderedDict[str, dict[str, Any]] = OrderedDict()
        self._max_sessions = max_sessions
        self._max_turns = max_turns_per_session

    def get_context(self, session_id: str | None) -> dict[str, Any]:
        if not session_id:
            return {"turns": [], "latestFinancialInput": None, "latestFinancialResult": None, "farmContext": {}}
        if session_id in self._sessions:
            self._sessions.move_to_end(session_id)
            return self._sessions[session_id]
        return {"turns": [], "latestFinancialInput": None, "latestFinancialResult": None, "farmContext": {}}

    def update_context(
        self,
        session_id: str | None,
        *,
        user_question: str,
        intent: str,
        assistant_answer: str,
        financial_input: dict[str, Any] | None = None,
        financial_result: dict[str, Any] | None = None,
        farm_context: dict[str, Any] | None = None,
    ) -> None:
        if not session_id:
            return

        if session_id in self._sessions:
            ctx = self._sessions[session_id]
            self._sessions.move_to_end(session_id)
        else:
            if len(self._sessions) >= self._max_sessions:
                self._sessions.popitem(last=False)
            ctx = {"turns": [], "latestFinancialInput": None, "latestFinancialResult": None, "farmContext": {}}
            self._sessions[session_id] = ctx

        turns = ctx["turns"]
        turns.append({
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "question": user_question,
            "intent": intent,
            "answerSummary": assistant_answer[:500],
        })
        if len(turns) > self._max_turns:
            ctx["turns"] = turns[-self._max_turns:]

        if financial_input is not None:
            ctx["latestFinancialInput"] = financial_input
        if financial_result is not None:
            ctx["latestFinancialResult"] = financial_result
        if farm_context:
            ctx["farmContext"] = farm_context


session_context_store = SessionContextStore()
