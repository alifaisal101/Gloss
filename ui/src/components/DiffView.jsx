import { useState } from 'react';
import { parse } from 'diff2html';
import InlineComment from './InlineComment.jsx';
import CommentForm from './CommentForm.jsx';

export default function DiffView({ diff, comments, onEditComment, onDeleteComment, onAddComment }) {
  if (!diff) return <div className="empty">No diff available.</div>;

  const files = parse(diff);

  return (
    <div className="diff-view">
      {files.map(file => {
        const filePath = file.newName !== '/dev/null' ? file.newName : file.oldName;
        return (
          <FileDiff
            key={filePath}
            file={file}
            filePath={filePath}
            comments={comments.filter(c => c.filePath === filePath)}
            onEditComment={onEditComment}
            onDeleteComment={onDeleteComment}
            onAddComment={(lineNumber, body, reason) => onAddComment(filePath, lineNumber, body, reason)}
          />
        );
      })}
    </div>
  );
}

function FileDiff({ file, filePath, comments, onEditComment, onDeleteComment, onAddComment }) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className="file-diff">
      <div className="file-diff-header" onClick={() => setCollapsed(c => !c)}>
        <span className="collapse-icon">{collapsed ? '▶' : '▼'}</span>
        <span className="file-path">{filePath}</span>
        <span className="file-stats">
          {file.addedLines > 0 && <span className="stat-add">+{file.addedLines}</span>}
          {file.deletedLines > 0 && <span className="stat-del">-{file.deletedLines}</span>}
        </span>
      </div>

      {!collapsed && (
        <table className="diff-table">
          <tbody>
            {file.blocks.map((block, bi) => (
              <BlockRows
                key={bi}
                block={block}
                comments={comments}
                onEditComment={onEditComment}
                onDeleteComment={onDeleteComment}
                onAddComment={onAddComment}
              />
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function BlockRows({ block, comments, onEditComment, onDeleteComment, onAddComment }) {
  const [addingAtLine, setAddingAtLine] = useState(null);
  const rows = [];

  rows.push(
    <tr key="hunk" className="diff-hunk-row">
      <td className="line-num" />
      <td className="line-num" />
      <td className="diff-hunk">{block.header}</td>
    </tr>,
  );

  for (const line of block.lines) {
    const lineNum = line.newNumber ?? line.oldNumber;
    const lineKey = `${line.oldNumber ?? 'x'}-${line.newNumber ?? 'x'}`;

    const lineComments = comments.filter(c =>
      (line.newNumber != null && c.lineNumber === line.newNumber) ||
      (line.oldNumber != null && c.lineNumber === line.oldNumber),
    ).filter(c => c.state !== 'Deleted');

    rows.push(
      <tr key={lineKey} className={`diff-line diff-line-${line.type}`}>
        <td className="line-num old">{line.oldNumber}</td>
        <td className="line-num new">{line.newNumber}</td>
        <td className="line-content">
          <span className="line-sign">
            {line.type === 'insert' ? '+' : line.type === 'delete' ? '-' : ' '}
          </span>
          <span
            className="line-text"
            dangerouslySetInnerHTML={{ __html: escapeHtml(line.content.replace(/^[+\- ]/, '')) }}
          />
          {line.newNumber != null && (
            <button
              className="add-comment-btn"
              onClick={() => setAddingAtLine(n => n === line.newNumber ? null : line.newNumber)}
              title="Add comment at this line"
            >
              +
            </button>
          )}
        </td>
      </tr>,
    );

    for (const comment of lineComments) {
      rows.push(
        <tr key={`comment-${comment.id}`} className="comment-row">
          <td colSpan={3}>
            <InlineComment
              comment={comment}
              onEdit={(body, reason) => onEditComment(comment.id, body, reason)}
              onDelete={(reason) => onDeleteComment(comment.id, reason)}
            />
          </td>
        </tr>,
      );
    }

    if (addingAtLine === lineNum) {
      rows.push(
        <tr key={`add-${lineNum}`} className="comment-row">
          <td colSpan={3}>
            <CommentForm
              onSubmit={(body, reason) => {
                onAddComment(lineNum, body, reason);
                setAddingAtLine(null);
              }}
              onCancel={() => setAddingAtLine(null)}
            />
          </td>
        </tr>,
      );
    }
  }

  return rows;
}

function escapeHtml(str) {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}
