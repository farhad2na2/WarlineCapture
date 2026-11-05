"""Offline composition of existing validators; no Unity, queue mutation, claim or execution."""
from pathlib import Path
import argparse,hashlib,json,sys
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,required=True)
    parser.add_argument('--sampling-plan',type=Path)
    parser.add_argument('--sampling-sha')
    parser.add_argument('--job',type=Path)
    parser.add_argument('--native-identity',type=Path)
    args=parser.parse_args();root=args.project.resolve()
    sys.path.insert(0,str(root/'Tools/CI'))
    import build_skirmish_authoring_batches as author
    import skirmish_production_matrix_authority as matrix
    import skirmish_production_queue as queue
    import skirmish_native_identity as identity
    report=dict(scope='OfflineCsvSamplingAndSuppliedIdentityOnly',dataValid=False,
                unityEditModeExecuted=False,physicalSourceValidated=False,nativeAuthentication=False,fullQueuePreflightPerformed=False,
                missionAccepted=False,regular720Completed=False,stress1200Completed=False)
    try:
        catalog=author.rows(root/matrix.CATALOG)
        sizes=author.rows(root/'Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv')
        author.validate(catalog,sizes)
        report.update(catalogRows=len(catalog),canonicalSizeRows=len(sizes))
        queue.require((args.sampling_plan is None)==(args.sampling_sha is None),'Both sampling plan and SHA required')
        queue.require((args.job is None)==(args.native_identity is None),'Both supplied job and identity required')
        authority=None
        if args.sampling_plan is not None:
            authority=matrix.SamplingAuthority(root,args.sampling_plan,args.sampling_sha)
            report['samplingAuthority']=dict(kind=authority.kind,slots=len(authority.slots),executionCredit=0)
        if args.job is not None:
            queue.require(authority is not None,'Existing full sampling authority required for supplied job')
            job=matrix.strict(args.job);value=matrix.strict(args.native_identity)
            queue.identifier(job['job_id'])
            authority.validate_job(job)
            identity.verify_job(job,value)
            report['suppliedIdentityConsistency']=dict(job_sha256=queue.fingerprint(job),
                catalog=job['catalog_id'],seed=job['seed'],passed=True,actualNativeProvenanceVerified=False)
            report['fullQueuePreflightPerformed']=False
        if authority is not None:authority.recheck()
        report['dataValid']=True
    except (ValueError,KeyError,TypeError,OSError) as error:
        report['refusal']=str(error)
    print(json.dumps(report,indent=2,sort_keys=True))
    return 0 if report['dataValid'] else 1
if __name__=='__main__':raise SystemExit(main())
