-- StudyFlow: roles & explicitly assigned students.
-- Run AFTER 001_schema_supabase.sql in Supabase SQL Editor by a project admin.
-- Re-runnable. No plaintext credentials or real students included.
-- Never allow students to promote themselves via a public API.

create table if not exists public.sf_user_roles (
    user_id uuid primary key references auth.users(id) on delete cascade,
    role text not null default 'Student'
        check (role in ('Student', 'Curator')),
    created_at timestamptz not null default now()
);

alter table public.sf_user_roles enable row level security;
revoke all on public.sf_user_roles from public, anon, authenticated;
grant select on public.sf_user_roles to authenticated;
drop policy if exists sf_user_roles_read_self on public.sf_user_roles;
create policy sf_user_roles_read_self on public.sf_user_roles
    for select to authenticated
    using (user_id = (select auth.uid()));

-- New signups always start as Student. This does NOT read user-editable role metadata.
create or replace function public.sf_create_default_role()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    insert into public.sf_user_roles(user_id, role)
    values (new.id, 'Student')
    on conflict (user_id) do nothing;
    return new;
end;
$$;
revoke all on function public.sf_create_default_role() from public, anon, authenticated;
drop trigger if exists sf_create_default_role_on_signup on auth.users;
create trigger sf_create_default_role_on_signup
    after insert on auth.users
    for each row execute function public.sf_create_default_role();

-- Existing accounts get a Student role unless the admin assigned one already.
insert into public.sf_user_roles(user_id, role)
select id, 'Student' from auth.users
on conflict (user_id) do nothing;

-- Only the project admin can create/delete curator -> student assignments.
create table if not exists public.sf_curator_students (
    curator_id uuid not null references auth.users(id) on delete cascade,
    student_id uuid not null references auth.users(id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (curator_id, student_id),
    constraint sf_curator_not_self check (curator_id <> student_id)
);

create index if not exists sf_curator_students_student_idx
    on public.sf_curator_students(student_id);
alter table public.sf_curator_students enable row level security;
revoke all on public.sf_curator_students from public, anon, authenticated;
grant select on public.sf_curator_students to authenticated;
drop policy if exists sf_curator_students_read_assignment on public.sf_curator_students;
create policy sf_curator_students_read_assignment on public.sf_curator_students
    for select to authenticated
    using (curator_id = (select auth.uid()) or student_id = (select auth.uid()));

-- Extra SELECT permissions ONLY for a curator's explicitly assigned students.
-- Existing owner-only INSERT/UPDATE/DELETE policies from migration 001 are unchanged.
drop policy if exists sf_curator_read_assigned_profiles on public.profiles;
create policy sf_curator_read_assigned_profiles on public.profiles
    for select to authenticated using (
        exists (
            select 1 from public.sf_curator_students cs
            join public.sf_user_roles r on r.user_id = cs.curator_id
            where cs.curator_id = (select auth.uid())
              and cs.student_id = profiles.id
              and r.role = 'Curator'
        )
    );

drop policy if exists sf_curator_read_assigned_tasks on public.sf_tasks;
create policy sf_curator_read_assigned_tasks on public.sf_tasks
    for select to authenticated using (
        exists (
            select 1 from public.sf_curator_students cs
            join public.sf_user_roles r on r.user_id = cs.curator_id
            where cs.curator_id = (select auth.uid())
              and cs.student_id = sf_tasks.owner_id
              and r.role = 'Curator'
        )
    );

drop policy if exists sf_curator_read_assigned_exams on public.sf_practice_exams;
create policy sf_curator_read_assigned_exams on public.sf_practice_exams
    for select to authenticated using (
        exists (
            select 1 from public.sf_curator_students cs
            join public.sf_user_roles r on r.user_id = cs.curator_id
            where cs.curator_id = (select auth.uid())
              and cs.student_id = sf_practice_exams.owner_id
              and r.role = 'Curator'
        )
    );

drop policy if exists sf_curator_read_assigned_scores on public.sf_subject_scores;
create policy sf_curator_read_assigned_scores on public.sf_subject_scores
    for select to authenticated using (
        exists (
            select 1 from public.sf_curator_students cs
            join public.sf_user_roles r on r.user_id = cs.curator_id
            where cs.curator_id = (select auth.uid())
              and cs.student_id = sf_subject_scores.owner_id
              and r.role = 'Curator'
        )
    );

-- IMPORTANT: Never add INSERT/UPDATE/DELETE policies for authenticated users
-- on sf_user_roles or sf_curator_students. Only project administrators assign roles.
